"use client";

import { useRouter } from "next/navigation";
import { useMemo, useState, type FormEvent, type ReactNode } from "react";
import { toast } from "sonner";
import {
  CamposEndereco,
  enderecoDeApi,
  enderecoParaApi,
  selectClass,
  type EnderecoFormulario,
} from "@/components/imoveis/campos-endereco";
import { StatusBadge } from "@/components/data/status-badge";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { bff, BffError, type AtualizacaoClienteInput } from "@/lib/api/browser";
import type { AuthMe, CertificadoDigital, Cliente, Plano } from "@/lib/api/types";
import { formatarCpfCnpj, formatarData, mascaraTelefone } from "@/lib/format";
import { beneficiosDoPlano, precoRotulo } from "@/lib/planos";
import { cn } from "@/lib/utils";

const REGIMES = [
  { valor: "SimplesNacional", rotulo: "Simples Nacional" },
  { valor: "LucroPresumido", rotulo: "Lucro Presumido" },
  { valor: "LucroReal", rotulo: "Lucro Real" },
  { valor: "Mei", rotulo: "MEI" },
] as const;

export function FormularioMinhaConta({
  me,
  cliente: clienteInicial,
  planos,
  certificado: certificadoInicial,
}: {
  me: AuthMe;
  cliente: Cliente;
  planos: Plano[];
  certificado: CertificadoDigital | null;
}) {
  const router = useRouter();
  const [cliente, setCliente] = useState(clienteInicial);
  const [certificado, setCertificado] = useState(certificadoInicial);
  const [endereco, setEndereco] = useState<EnderecoFormulario>(enderecoDeApi(clienteInicial.endereco));
  const [telefone, setTelefone] = useState(clienteInicial.telefone ? mascaraTelefone(clienteInicial.telefone) : "");
  const [pending, setPending] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [planoModal, setPlanoModal] = useState<Plano | null>(null);

  const [arquivoCert, setArquivoCert] = useState<File | null>(null);
  const [senhaCert, setSenhaCert] = useState("");
  const [enviandoCert, setEnviandoCert] = useState(false);

  const planoVigente = useMemo(
    () => planos.find((p) => p.id === cliente.planoId) ?? null,
    [planos, cliente.planoId],
  );
  const mostraNfse = Boolean(planoVigente?.permiteNfse);
  const ehPj = cliente.tipoPessoa === "PJ";

  async function onSubmit(ev: FormEvent<HTMLFormElement>) {
    ev.preventDefault();
    setErro(null);
    setPending(true);
    const form = new FormData(ev.currentTarget);

    const dados: AtualizacaoClienteInput = {
      planoId: cliente.planoId,
      telefone: telefone.replace(/\D/g, "") || null,
      email: String(form.get("email") ?? "").trim() || null,
      endereco: enderecoParaApi(endereco),
      cnaePrincipal: mostraNfse ? String(form.get("cnaePrincipal") ?? "").trim() || null : cliente.cnaePrincipal,
      codigoServico: mostraNfse ? String(form.get("codigoServico") ?? "").trim() || null : cliente.codigoServico,
      regimeTributario: mostraNfse
        ? String(form.get("regimeTributario") ?? "").trim() || null
        : cliente.regimeTributario,
    };

    if (ehPj) {
      dados.razaoSocial = String(form.get("razaoSocial") ?? "").trim() || null;
      dados.nomeFantasia = String(form.get("nomeFantasia") ?? "").trim() || null;
      dados.inscricaoMunicipal = String(form.get("inscricaoMunicipal") ?? "").trim() || null;
    } else {
      dados.nome = String(form.get("nome") ?? "").trim() || null;
      const nasc = String(form.get("dataNascimento") ?? "").trim();
      dados.dataNascimento = nasc || null;
    }

    try {
      const atualizado = await bff.atualizarCliente(cliente.id, dados);
      setCliente(atualizado);
      setEndereco(enderecoDeApi(atualizado.endereco));
      setTelefone(atualizado.telefone ? mascaraTelefone(atualizado.telefone) : "");
      toast.success("Dados da conta atualizados.");
      router.refresh();
    } catch (e) {
      const msg = e instanceof BffError ? e.message : "Não foi possível salvar.";
      setErro(msg);
      toast.error(msg);
    } finally {
      setPending(false);
    }
  }

  async function onEnviarCertificado() {
    if (!arquivoCert) {
      toast.error("Selecione o arquivo PFX/P12 do certificado A1.");
      return;
    }
    if (!senhaCert.trim()) {
      toast.error("Informe a senha do certificado.");
      return;
    }
    setEnviandoCert(true);
    try {
      const dto = await bff.enviarCertificado(cliente.id, arquivoCert, senhaCert);
      setCertificado(dto);
      setArquivoCert(null);
      setSenhaCert("");
      toast.success("Certificado A1 enviado com sucesso.");
      router.refresh();
    } catch (e) {
      toast.error(e instanceof BffError ? e.message : "Falha ao enviar o certificado.");
    } finally {
      setEnviandoCert(false);
    }
  }

  function onCliquePlano(plano: Plano) {
    if (plano.id === cliente.planoId) return;
    setPlanoModal(plano);
  }

  return (
    <>
      <form onSubmit={(ev) => void onSubmit(ev)} className="space-y-4">
        <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
          <h3 className="mb-1 text-sm font-semibold">Usuário</h3>
          <p className="mb-4 text-xs text-muted-foreground">Identidade da sessão (somente leitura).</p>
          <dl className="grid gap-3 text-sm sm:grid-cols-2">
            <Linha rotulo="Nome" valor={me.nome} />
            <Linha rotulo="E-mail" valor={me.email} />
            <Linha
              rotulo="Perfil"
              valor={
                <span className="flex flex-wrap gap-1">
                  {me.roles.map((r) => (
                    <StatusBadge key={r} status={r} />
                  ))}
                </span>
              }
            />
            <Linha rotulo="Status do cliente" valor={<StatusBadge status={cliente.status} />} />
          </dl>
        </section>

        <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
          <h3 className="mb-1 text-sm font-semibold">Dados cadastrais do cliente</h3>
          <p className="mb-4 text-xs text-muted-foreground">
            Locador / emitente. CPF/CNPJ e tipo de pessoa não podem ser alterados.
          </p>
          <div className="grid gap-4 sm:grid-cols-2">
            {ehPj ? (
              <>
                <Campo rotulo="Razão social" htmlFor="razaoSocial">
                  <Input
                    id="razaoSocial"
                    name="razaoSocial"
                    required
                    defaultValue={cliente.razaoSocial ?? ""}
                    className="h-10 rounded-[4px]"
                  />
                </Campo>
                <Campo rotulo="Nome fantasia" htmlFor="nomeFantasia">
                  <Input
                    id="nomeFantasia"
                    name="nomeFantasia"
                    defaultValue={cliente.nomeFantasia ?? ""}
                    className="h-10 rounded-[4px]"
                  />
                </Campo>
                <Campo rotulo="CNPJ" htmlFor="cnpj">
                  <Input
                    id="cnpj"
                    readOnly
                    value={formatarCpfCnpj(cliente.cnpj ?? "")}
                    className="h-10 rounded-[4px] bg-muted/40"
                  />
                </Campo>
                <Campo rotulo="Inscrição municipal" htmlFor="inscricaoMunicipal">
                  <Input
                    id="inscricaoMunicipal"
                    name="inscricaoMunicipal"
                    defaultValue={cliente.inscricaoMunicipal ?? ""}
                    className="h-10 rounded-[4px]"
                  />
                </Campo>
              </>
            ) : (
              <>
                <Campo rotulo="Nome" htmlFor="nome">
                  <Input
                    id="nome"
                    name="nome"
                    required
                    defaultValue={cliente.nome ?? ""}
                    className="h-10 rounded-[4px]"
                  />
                </Campo>
                <Campo rotulo="CPF" htmlFor="cpf">
                  <Input
                    id="cpf"
                    readOnly
                    value={formatarCpfCnpj(cliente.cpf ?? "")}
                    className="h-10 rounded-[4px] bg-muted/40"
                  />
                </Campo>
                <Campo rotulo="Data de nascimento" htmlFor="dataNascimento">
                  <Input
                    id="dataNascimento"
                    name="dataNascimento"
                    type="date"
                    defaultValue={cliente.dataNascimento?.slice(0, 10) ?? ""}
                    className="h-10 rounded-[4px]"
                  />
                </Campo>
              </>
            )}
            <Campo rotulo="E-mail" htmlFor="email">
              <Input
                id="email"
                name="email"
                type="email"
                defaultValue={cliente.email ?? ""}
                className="h-10 rounded-[4px]"
              />
            </Campo>
            <Campo rotulo="Telefone" htmlFor="telefone">
              <Input
                id="telefone"
                name="telefone"
                value={telefone}
                onChange={(e) => setTelefone(mascaraTelefone(e.target.value))}
                className="h-10 rounded-[4px]"
              />
            </Campo>
            <div className="sm:col-span-2">
              <CamposEndereco value={endereco} onChange={setEndereco} />
            </div>
          </div>
        </section>

        <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
          <h3 className="mb-1 text-sm font-semibold">Plano de assinatura</h3>
          <p className="mb-4 text-xs text-muted-foreground">
            Plano vigente{planoVigente ? `: ${planoVigente.nome}` : ""}. Clique em outro plano para ver os serviços e o
            link de pagamento.
          </p>
          <ul className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
            {planos.map((p) => {
              const vigente = p.id === cliente.planoId;
              return (
                <li key={p.id}>
                  <button
                    type="button"
                    onClick={() => onCliquePlano(p)}
                    className={cn(
                      "flex h-full w-full flex-col rounded-[4px] border p-3 text-left transition-colors",
                      vigente
                        ? "border-primary bg-[#F4E4DC]/70 ring-1 ring-primary/40 dark:bg-accent"
                        : "border-border hover:border-primary/50",
                    )}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <p className="font-semibold">{p.nome}</p>
                      {vigente ? <StatusBadge status="Vigente" /> : null}
                    </div>
                    <p className="mt-1 text-sm font-medium text-primary">{precoRotulo(p)}</p>
                    <p className="mt-2 text-xs text-muted-foreground">
                      Até {p.maxImoveis ?? "∞"} imóveis · {p.maxUsuarios ?? "∞"} usuários
                      {p.permiteNfse ? " · Emite NFS-e" : ""}
                    </p>
                  </button>
                </li>
              );
            })}
          </ul>
        </section>

        {mostraNfse ? (
          <section className="rounded-lg bg-card p-5 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
            <h3 className="mb-1 text-sm font-semibold">Dados para emissão de NFS-e</h3>
            <p className="mb-4 text-xs text-muted-foreground">
              Exigidos pelo plano {planoVigente?.nome} (emissão de NFS-e habilitada). A senha do certificado nunca é
              exibida após o envio.
            </p>
            <div className="grid gap-4 sm:grid-cols-3">
              <Campo rotulo="CNAE" htmlFor="cnaePrincipal">
                <Input
                  id="cnaePrincipal"
                  name="cnaePrincipal"
                  placeholder="Ex.: 6821801"
                  defaultValue={cliente.cnaePrincipal ?? ""}
                  className="h-10 rounded-[4px]"
                />
              </Campo>
              <Campo rotulo="Código de Serviço" htmlFor="codigoServico">
                <Input
                  id="codigoServico"
                  name="codigoServico"
                  placeholder="Ex.: 07.02"
                  defaultValue={cliente.codigoServico ?? ""}
                  className="h-10 rounded-[4px]"
                />
              </Campo>
              <Campo rotulo="Regime Tributário" htmlFor="regimeTributario">
                <select
                  id="regimeTributario"
                  name="regimeTributario"
                  defaultValue={cliente.regimeTributario ?? ""}
                  className={selectClass}
                >
                  <option value="">Selecione</option>
                  {REGIMES.map((r) => (
                    <option key={r.valor} value={r.valor}>
                      {r.rotulo}
                    </option>
                  ))}
                </select>
              </Campo>
            </div>

            <div className="mt-5 rounded-[4px] border border-border bg-[#F8F9FA] p-4 dark:bg-muted/40">
              <p className="text-xs font-medium tracking-wide text-muted-foreground uppercase">
                Certificado digital A1
              </p>
              {certificado ? (
                <dl className="mt-2 grid gap-2 text-sm sm:grid-cols-2">
                  <Linha rotulo="Thumbprint" valor={<span className="break-all font-mono text-xs">{certificado.thumbprint}</span>} />
                  <Linha
                    rotulo="Validade"
                    valor={
                      <span className={certificado.vencido ? "text-destructive" : undefined}>
                        {formatarData(certificado.validade)}
                        {certificado.vencido ? " (vencido)" : ""}
                      </span>
                    }
                  />
                </dl>
              ) : (
                <p className="mt-2 text-sm text-muted-foreground">Nenhum certificado A1 ativo cadastrado.</p>
              )}
              <div className="mt-3 grid gap-4 sm:grid-cols-2">
                <Campo rotulo="Arquivo PFX/P12" htmlFor="certificadoArquivo">
                  <Input
                    id="certificadoArquivo"
                    type="file"
                    accept=".pfx,.p12,application/x-pkcs12"
                    className="h-10 rounded-[4px] pt-1.5"
                    onChange={(e) => setArquivoCert(e.target.files?.[0] ?? null)}
                  />
                </Campo>
                <Campo rotulo="Senha do certificado" htmlFor="certificadoSenha">
                  <Input
                    id="certificadoSenha"
                    type="password"
                    autoComplete="new-password"
                    value={senhaCert}
                    onChange={(e) => setSenhaCert(e.target.value)}
                    className="h-10 rounded-[4px]"
                    placeholder="Senha do PFX"
                  />
                </Campo>
              </div>
              <div className="mt-3 flex justify-end">
                <Button
                  type="button"
                  variant="outline"
                  className="h-9 rounded-[4px]"
                  disabled={enviandoCert}
                  onClick={() => void onEnviarCertificado()}
                >
                  {enviandoCert ? "Enviando…" : certificado ? "Substituir certificado" : "Enviar certificado"}
                </Button>
              </div>
            </div>
          </section>
        ) : null}

        {erro ? <p className="text-sm text-destructive">{erro}</p> : null}

        <div className="flex justify-end">
          <Button type="submit" disabled={pending} className="h-9 rounded-[4px] px-5">
            {pending ? "Salvando…" : "Salvar alterações"}
          </Button>
        </div>
      </form>

      <Dialog open={planoModal !== null} onOpenChange={(aberto) => { if (!aberto) setPlanoModal(null); }}>
        {planoModal ? (
          <DialogContent className="sm:max-w-md">
            <div className="m-[10px] flex flex-col">
              <DialogHeader className="border-0 p-0 pr-10">
                <DialogTitle>Plano {planoModal.nome}</DialogTitle>
                <DialogDescription>
                  Detalhes dos serviços. A contratação com pagamento online será habilitada em breve.
                </DialogDescription>
              </DialogHeader>
              <div className="py-3">
                <p className="text-lg font-semibold text-primary">{precoRotulo(planoModal)}</p>
                <ul className="mt-3 list-disc space-y-1.5 pl-5 text-sm">
                  {beneficiosDoPlano(planoModal).map((item) => (
                    <li key={item}>{item}</li>
                  ))}
                </ul>
                <p className="mt-4 text-xs text-muted-foreground">
                  Cobrança destinada à Lucrare Contabilidade Estratégia LTDA — CNPJ 51.095.456/0001-13.
                </p>
              </div>
              <DialogFooter className="border-0 p-0">
                <Button
                  type="button"
                  variant="secondary"
                  className="h-9 rounded-[4px] bg-[#95A5A6] text-white hover:bg-[#7F8C8D]"
                  onClick={() => setPlanoModal(null)}
                >
                  Fechar
                </Button>
                <Button
                  type="button"
                  className="h-9 rounded-[4px]"
                  onClick={() => {
                    toast.info("O link de pagamento (Mercado Pago) estará disponível em breve.");
                  }}
                >
                  Ir para pagamento
                </Button>
              </DialogFooter>
            </div>
          </DialogContent>
        ) : null}
      </Dialog>
    </>
  );
}

function Campo({
  rotulo,
  htmlFor,
  children,
}: {
  rotulo: string;
  htmlFor: string;
  children: ReactNode;
}) {
  return (
    <div className="flex flex-col gap-1.5">
      <Label htmlFor={htmlFor} className="text-[#333] dark:text-foreground">
        {rotulo}
      </Label>
      {children}
    </div>
  );
}

function Linha({ rotulo, valor }: { rotulo: string; valor: ReactNode }) {
  return (
    <div className="flex items-start justify-between gap-4 border-b border-muted py-2 last:border-0">
      <dt className="text-muted-foreground">{rotulo}</dt>
      <dd className="text-right font-medium">{valor}</dd>
    </div>
  );
}

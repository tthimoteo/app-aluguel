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
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { bff, BffError, type AtualizacaoClienteInput } from "@/lib/api/browser";
import type { AuthMe, Cliente, Plano } from "@/lib/api/types";
import { formatarCpfCnpj, formatarMoeda, mascaraTelefone } from "@/lib/format";
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
}: {
  me: AuthMe;
  cliente: Cliente;
  planos: Plano[];
}) {
  const router = useRouter();
  const [cliente, setCliente] = useState(clienteInicial);
  const [planoId, setPlanoId] = useState(clienteInicial.planoId ?? "");
  const [endereco, setEndereco] = useState<EnderecoFormulario>(enderecoDeApi(clienteInicial.endereco));
  const [telefone, setTelefone] = useState(clienteInicial.telefone ? mascaraTelefone(clienteInicial.telefone) : "");
  const [pending, setPending] = useState(false);
  const [erro, setErro] = useState<string | null>(null);

  const planoSelecionado = useMemo(
    () => planos.find((p) => p.id === planoId) ?? null,
    [planos, planoId],
  );
  const planoVigente = useMemo(
    () => planos.find((p) => p.id === cliente.planoId) ?? null,
    [planos, cliente.planoId],
  );
  const mostraNfse = Boolean(planoSelecionado?.permiteNfse);
  const ehPj = cliente.tipoPessoa === "PJ";

  async function onSubmit(ev: FormEvent<HTMLFormElement>) {
    ev.preventDefault();
    setErro(null);
    setPending(true);
    const form = new FormData(ev.currentTarget);

    const dados: AtualizacaoClienteInput = {
      planoId: planoId || null,
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
      setPlanoId(atualizado.planoId ?? "");
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

  return (
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
          Plano vigente{planoVigente ? `: ${planoVigente.nome}` : ""}. Selecione outro plano para alterar na próxima
          gravação.
        </p>
        <ul className="grid gap-3 md:grid-cols-2 xl:grid-cols-3">
          {planos.map((p) => {
            const selecionado = p.id === planoId;
            const vigente = p.id === cliente.planoId;
            return (
              <li key={p.id}>
                <button
                  type="button"
                  onClick={() => setPlanoId(p.id)}
                  className={cn(
                    "flex h-full w-full flex-col rounded-[4px] border p-3 text-left transition-colors",
                    selecionado
                      ? "border-primary bg-[#F4E4DC]/70 ring-1 ring-primary/40 dark:bg-accent"
                      : "border-border hover:border-primary/50",
                  )}
                >
                  <div className="flex items-start justify-between gap-2">
                    <p className="font-semibold">{p.nome}</p>
                    {vigente ? <StatusBadge status="Vigente" /> : null}
                  </div>
                  <p className="mt-1 text-sm font-medium text-primary">
                    {p.valorMensal != null ? formatarMoeda(p.valorMensal) : "Sob consulta"}
                    {p.valorMensal != null ? <span className="text-xs font-normal text-muted-foreground"> /mês</span> : null}
                  </p>
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
            Exigidos pelo plano {planoSelecionado?.nome} (emissão de NFS-e habilitada).
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
        </section>
      ) : null}

      {erro ? <p className="text-sm text-destructive">{erro}</p> : null}

      <div className="flex justify-end">
        <Button type="submit" disabled={pending} className="h-9 rounded-[4px] px-5">
          {pending ? "Salvando…" : "Salvar alterações"}
        </Button>
      </div>
    </form>
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

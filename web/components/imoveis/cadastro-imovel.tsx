"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, type FormEvent, type ReactNode } from "react";
import { toast } from "sonner";
import {
  CamposEndereco,
  enderecoDeApi,
  enderecoParaApi,
  enderecoVazio,
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
import {
  bff,
  BffError,
  type AtualizacaoContratoInput,
  type AtualizacaoInquilinoInput,
  type NovoContratoInput,
  type NovoInquilinoInput,
} from "@/lib/api/browser";
import type { Contrato, Imovel, Inquilino } from "@/lib/api/types";
import { formatarCpfCnpj, formatarData, formatarEndereco, formatarMoeda } from "@/lib/format";
import { cn } from "@/lib/utils";

type Painel =
  | { tipo: "inquilino-criar" }
  | { tipo: "inquilino-editar" }
  | { tipo: "inquilino-remover" }
  | { tipo: "contrato-criar" | "contrato-renovar" }
  | { tipo: "contrato-editar" }
  | { tipo: "contrato-encerrar" };

export function CadastroImovel({
  imovel,
  inquilino: inquilinoProp,
  contratoAtivo,
  inquilinosCliente,
  podeGerenciar,
}: {
  imovel: Imovel;
  inquilino: Inquilino | null;
  contratoAtivo: Contrato | null;
  inquilinosCliente: Inquilino[];
  podeGerenciar: boolean;
}) {
  const router = useRouter();
  const [painel, setPainel] = useState<Painel | null>(null);
  const [erro, setErro] = useState<string | null>(null);
  const [pending, setPending] = useState(false);
  const [enderecoInq, setEnderecoInq] = useState<EnderecoFormulario>(enderecoVazio());
  const [inquilinoLocal, setInquilinoLocal] = useState<Inquilino | null>(null);
  const inquilino = inquilinoProp ?? inquilinoLocal;

  function fechar() {
    setPainel(null);
    setErro(null);
  }

  function recarregar() {
    router.refresh();
  }

  async function onCriarInquilino(form: FormData) {
    setErro(null);
    const dados: NovoInquilinoInput = {
      clienteId: imovel.clienteId,
      tipoPessoa: form.get("tipoPessoa") === "PJ" ? "PJ" : "PF",
      nome: String(form.get("nome") ?? "").trim(),
      documento: String(form.get("documento") ?? "").trim(),
      inscricaoMunicipal: textoOuNulo(form.get("inscricaoMunicipal")),
      telefone: textoOuNulo(form.get("telefone")),
      email: textoOuNulo(form.get("email")),
      endereco: enderecoParaApi(enderecoInq),
    };
    setPending(true);
    try {
      const criado = await bff.criarInquilino(dados);
      setInquilinoLocal(criado);
      toast.success("Inquilino incluído.");
      fechar();
      router.replace(`/imoveis/${imovel.id}?inquilinoId=${criado.id}`);
      recarregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Não foi possível incluir o inquilino.");
    } finally {
      setPending(false);
    }
  }

  async function onEditarInquilino(form: FormData) {
    if (!inquilino) return;
    setErro(null);
    const dados: AtualizacaoInquilinoInput = {
      nome: String(form.get("nome") ?? "").trim(),
      inscricaoMunicipal: textoOuNulo(form.get("inscricaoMunicipal")),
      telefone: textoOuNulo(form.get("telefone")),
      email: textoOuNulo(form.get("email")),
      status: form.get("status") === "Inativo" ? "Inativo" : "Ativo",
      endereco: enderecoParaApi(enderecoInq),
    };
    setPending(true);
    try {
      await bff.atualizarInquilino(inquilino.id, dados);
      toast.success("Inquilino atualizado.");
      fechar();
      recarregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Não foi possível atualizar o inquilino.");
    } finally {
      setPending(false);
    }
  }

  async function onRemoverInquilino() {
    if (!inquilino) return;
    setErro(null);
    setPending(true);
    try {
      await bff.removerInquilino(inquilino.id);
      setInquilinoLocal(null);
      toast.success("Inquilino removido.");
      fechar();
      router.replace(`/imoveis/${imovel.id}`);
      recarregar();
    } catch (e) {
      setErro(e instanceof BffError || e instanceof Error ? e.message : "Não foi possível remover o inquilino.");
    } finally {
      setPending(false);
    }
  }

  async function onSalvarContrato(form: FormData, renovar: boolean) {
    setErro(null);
    const dados: NovoContratoInput = {
      imovelId: imovel.id,
      inquilinoId: String(form.get("inquilinoId") ?? inquilino?.id ?? ""),
      numeroContrato: String(form.get("numeroContrato") ?? "").trim(),
      dataInicio: String(form.get("dataInicio") ?? ""),
      dataFimPrevista: textoOuNulo(form.get("dataFimPrevista")),
      diaVencimento: Number(form.get("diaVencimento") || 10),
      valorAluguel: Number(String(form.get("valorAluguel") ?? "").replace(",", ".")),
      jurosAtrasoPct: numeroOuNulo(form.get("jurosAtrasoPct")),
      multaAtrasoPct: numeroOuNulo(form.get("multaAtrasoPct")),
    };
    setPending(true);
    try {
      if (renovar && contratoAtivo) {
        await bff.encerrarContrato(contratoAtivo.id);
      }
      await bff.criarContrato(dados);
      toast.success(renovar ? "Contrato renovado." : "Contrato incluído.");
      fechar();
      recarregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Não foi possível salvar o contrato.");
    } finally {
      setPending(false);
    }
  }

  async function onEditarContrato(form: FormData) {
    if (!contratoAtivo) return;
    setErro(null);
    const dados: AtualizacaoContratoInput = {
      dataFimPrevista: textoOuNulo(form.get("dataFimPrevista")),
      diaVencimento: Number(form.get("diaVencimento") || contratoAtivo.diaVencimento),
      valorAluguel: Number(String(form.get("valorAluguel") ?? "").replace(",", ".")),
      jurosAtrasoPct: numeroOuNulo(form.get("jurosAtrasoPct")),
      multaAtrasoPct: numeroOuNulo(form.get("multaAtrasoPct")),
    };
    setPending(true);
    try {
      await bff.atualizarContrato(contratoAtivo.id, dados);
      toast.success("Contrato atualizado.");
      fechar();
      recarregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Não foi possível atualizar o contrato.");
    } finally {
      setPending(false);
    }
  }

  async function onEncerrarContrato() {
    if (!contratoAtivo) return;
    setErro(null);
    setPending(true);
    try {
      await bff.encerrarContrato(contratoAtivo.id);
      toast.success("Contrato encerrado. O imóvel pode receber um novo contrato.");
      fechar();
      recarregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Não foi possível encerrar o contrato.");
    } finally {
      setPending(false);
    }
  }

  const listaInquilinos = inquilino
    ? [inquilino, ...inquilinosCliente.filter((i) => i.id !== inquilino.id)]
    : inquilinosCliente;

  return (
    <div className="space-y-6">
      <p>
        <Link
          href={`/imoveis?clienteId=${imovel.clienteId}`}
          className="text-sm font-medium text-primary hover:text-primary/80"
        >
          ← Imóveis
        </Link>
      </p>

      <section className="rounded-lg bg-card p-6 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
        <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 className="text-xl font-semibold">{imovel.nome}</h2>
            <p className="mt-1 text-sm text-muted-foreground">Cadastro do imóvel (UC003).</p>
          </div>
          <StatusBadge status={imovel.status} />
        </div>
        <dl className="grid gap-4 sm:grid-cols-2">
          <CampoDetalhe rotulo="Tipo" valor={rotuloTipo(imovel.tipo)} />
          <CampoDetalhe rotulo="Nr. IPTU" valor={imovel.numeroIptu ?? "—"} />
          <CampoDetalhe rotulo="Nr. Matrícula" valor={imovel.numeroMatricula ?? "—"} />
          <CampoDetalhe rotulo="CEP" valor={imovel.endereco.cep ?? "—"} />
          <CampoDetalhe rotulo="Endereço" valor={formatarEndereco(imovel.endereco)} />
        </dl>
      </section>

      <section className="rounded-lg bg-card p-6 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
        <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
          <div>
            <h3 className="text-sm font-semibold">Inquilino</h3>
            <p className="mt-1 text-xs text-muted-foreground">Um inquilino por vez neste imóvel.</p>
          </div>
          {podeGerenciar ? (
            <div className="flex flex-wrap gap-2">
              {inquilino ? (
                <>
                  <Button
                    type="button"
                    className="h-8 rounded-[4px] px-3 text-xs"
                    onClick={() => {
                      setErro(null);
                      setEnderecoInq(enderecoDeApi(inquilino.endereco));
                      setPainel({ tipo: "inquilino-editar" });
                    }}
                  >
                    Editar
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    className="h-8 rounded-[4px] px-3 text-xs"
                    disabled={!!contratoAtivo}
                    title={contratoAtivo ? "Encerre o contrato antes de remover o inquilino." : undefined}
                    onClick={() => {
                      setErro(null);
                      setPainel({ tipo: "inquilino-remover" });
                    }}
                  >
                    Remover
                  </Button>
                </>
              ) : (
                <Button
                  type="button"
                  className="h-8 rounded-[4px] px-3 text-xs"
                  onClick={() => {
                    setErro(null);
                    setEnderecoInq(enderecoVazio());
                    setPainel({ tipo: "inquilino-criar" });
                  }}
                >
                  Incluir inquilino
                </Button>
              )}
            </div>
          ) : null}
        </div>
        {inquilino ? (
          <dl className="grid gap-4 sm:grid-cols-2">
            <CampoDetalhe rotulo="Nome" valor={inquilino.nome} />
            <CampoDetalhe rotulo="Tipo" valor={inquilino.tipoPessoa === "PJ" ? "Pessoa jurídica" : "Pessoa física"} />
            <CampoDetalhe rotulo="Documento" valor={formatarCpfCnpj(inquilino.documento)} />
            <CampoDetalhe rotulo="E-mail" valor={inquilino.email ?? "—"} />
            <CampoDetalhe rotulo="Telefone" valor={inquilino.telefone ?? "—"} />
            <CampoDetalhe rotulo="Status" valor={<StatusBadge status={inquilino.status} />} />
          </dl>
        ) : (
          <p className="text-sm text-muted-foreground">Nenhum inquilino vinculado. Inclua o locatário para emitir NFS-e e gerar o contrato.</p>
        )}
      </section>

      <section className="rounded-lg bg-card p-6 shadow-[0_2px_8px_rgba(0,0,0,0.1)] ring-1 ring-border">
        <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
          <div>
            <h3 className="text-sm font-semibold">Contrato</h3>
            <p className="mt-1 text-xs text-muted-foreground">Um contrato ativo por imóvel (CASO 3). Encerrar libera renovação.</p>
          </div>
          {podeGerenciar ? (
            <div className="flex flex-wrap gap-2">
              {contratoAtivo ? (
                <>
                  <Button
                    type="button"
                    className="h-8 rounded-[4px] px-3 text-xs"
                    onClick={() => {
                      setErro(null);
                      setPainel({ tipo: "contrato-editar" });
                    }}
                  >
                    Editar
                  </Button>
                  <Button
                    type="button"
                    className="h-8 rounded-[4px] px-3 text-xs"
                    onClick={() => {
                      setErro(null);
                      setPainel({ tipo: "contrato-renovar" });
                    }}
                  >
                    Renovar
                  </Button>
                  <Button
                    type="button"
                    variant="outline"
                    className="h-8 rounded-[4px] px-3 text-xs"
                    onClick={() => {
                      setErro(null);
                      setPainel({ tipo: "contrato-encerrar" });
                    }}
                  >
                    Encerrar
                  </Button>
                </>
              ) : (
                <Button
                  type="button"
                  className="h-8 rounded-[4px] px-3 text-xs"
                  disabled={listaInquilinos.length === 0}
                  title={listaInquilinos.length === 0 ? "Inclua o inquilino antes do contrato." : undefined}
                  onClick={() => {
                    setErro(null);
                    setPainel({ tipo: "contrato-criar" });
                  }}
                >
                  Incluir contrato
                </Button>
              )}
            </div>
          ) : null}
        </div>
        {contratoAtivo ? (
          <dl className="grid gap-4 sm:grid-cols-2">
            <CampoDetalhe rotulo="Número" valor={contratoAtivo.numeroContrato} />
            <CampoDetalhe rotulo="Status" valor={<StatusBadge status={contratoAtivo.status} />} />
            <CampoDetalhe rotulo="Início" valor={formatarData(contratoAtivo.dataInicio)} />
            <CampoDetalhe rotulo="Fim previsto" valor={formatarData(contratoAtivo.dataFimPrevista)} />
            <CampoDetalhe rotulo="Vencimento" valor={`Dia ${contratoAtivo.diaVencimento}`} />
            <CampoDetalhe rotulo="Aluguel" valor={formatarMoeda(contratoAtivo.valorAluguel)} />
          </dl>
        ) : (
          <p className="text-sm text-muted-foreground">Nenhum contrato ativo. Inclua ou renove após cadastrar o inquilino.</p>
        )}
      </section>

      <Dialog open={painel !== null} onOpenChange={(aberto) => { if (!aberto) fechar(); }}>
        {painel?.tipo === "inquilino-criar" || painel?.tipo === "inquilino-editar" ? (
          <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
            <form
              className="flex flex-col"
              onSubmit={(ev: FormEvent<HTMLFormElement>) => {
                ev.preventDefault();
                const form = new FormData(ev.currentTarget);
                if (painel.tipo === "inquilino-criar") void onCriarInquilino(form);
                else void onEditarInquilino(form);
              }}
            >
              <DialogHeader className="border-0 px-0 pt-0">
                <DialogTitle>{painel.tipo === "inquilino-criar" ? "Incluir inquilino" : "Editar inquilino"}</DialogTitle>
                <DialogDescription>Destinatário da NFS-e deste imóvel.</DialogDescription>
              </DialogHeader>
              <div className="grid gap-4 px-6 py-2 sm:grid-cols-2">
                {painel.tipo === "inquilino-criar" ? (
                  <Campo rotulo="Tipo" htmlFor="tipoPessoa">
                    <select id="tipoPessoa" name="tipoPessoa" defaultValue="PF" className={selectClass}>
                      <option value="PF">Pessoa física</option>
                      <option value="PJ">Pessoa jurídica</option>
                    </select>
                  </Campo>
                ) : (
                  <CampoDetalhe rotulo="Tipo" valor={inquilino?.tipoPessoa === "PJ" ? "Pessoa jurídica" : "Pessoa física"} />
                )}
                <Campo rotulo="Nome / Razão social" htmlFor="nomeInq">
                  <Input id="nomeInq" name="nome" required defaultValue={inquilino?.nome ?? ""} className="h-10 rounded-[4px]" />
                </Campo>
                {painel.tipo === "inquilino-criar" ? (
                  <Campo rotulo="CPF ou CNPJ" htmlFor="documento">
                    <Input id="documento" name="documento" required className="h-10 rounded-[4px]" />
                  </Campo>
                ) : (
                  <CampoDetalhe rotulo="Documento" valor={formatarCpfCnpj(inquilino?.documento)} />
                )}
                <Campo rotulo="E-mail" htmlFor="emailInq">
                  <Input id="emailInq" name="email" type="email" defaultValue={inquilino?.email ?? ""} className="h-10 rounded-[4px]" />
                </Campo>
                <Campo rotulo="Telefone" htmlFor="telefoneInq">
                  <Input id="telefoneInq" name="telefone" defaultValue={inquilino?.telefone ?? ""} className="h-10 rounded-[4px]" />
                </Campo>
                <Campo rotulo="Inscrição municipal" htmlFor="inscricaoMunicipal">
                  <Input id="inscricaoMunicipal" name="inscricaoMunicipal" defaultValue={inquilino?.inscricaoMunicipal ?? ""} className="h-10 rounded-[4px]" />
                </Campo>
                {painel.tipo === "inquilino-editar" ? (
                  <Campo rotulo="Status" htmlFor="statusInq">
                    <select id="statusInq" name="status" defaultValue={inquilino?.status ?? "Ativo"} className={selectClass}>
                      <option value="Ativo">Ativo</option>
                      <option value="Inativo">Inativo</option>
                    </select>
                  </Campo>
                ) : null}
                <div className="sm:col-span-2">
                  <p className="mb-2 text-sm font-medium">Endereço</p>
                  <CamposEndereco idPrefix="inquilino" value={enderecoInq} onChange={setEnderecoInq} />
                </div>
              </div>
              <AlertaErro mensagem={erro} />
              <DialogFooter className="border-0 px-0 pb-0">
                <Button type="button" variant="secondary" className="h-9 rounded-[4px] bg-[#95A5A6] text-white hover:bg-[#7F8C8D]" onClick={fechar}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={pending} className="h-9 rounded-[4px] px-4">
                  Salvar
                </Button>
              </DialogFooter>
            </form>
          </DialogContent>
        ) : null}

        {painel?.tipo === "inquilino-remover" ? (
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Remover inquilino</DialogTitle>
              <DialogDescription>{inquilino?.nome} será desativado neste cadastro.</DialogDescription>
            </DialogHeader>
            <AlertaErro mensagem={erro} />
            <DialogFooter>
              <Button type="button" variant="secondary" className="h-9 rounded-[4px] bg-[#95A5A6] text-white hover:bg-[#7F8C8D]" onClick={fechar}>
                Cancelar
              </Button>
              <Button type="button" disabled={pending} className="h-9 rounded-[4px] bg-[#E74C3C] px-4 text-white hover:bg-[#C0392B]" onClick={() => void onRemoverInquilino()}>
                Remover
              </Button>
            </DialogFooter>
          </DialogContent>
        ) : null}

        {painel?.tipo === "contrato-criar" || painel?.tipo === "contrato-renovar" || painel?.tipo === "contrato-editar" ? (
          <DialogContent className="max-h-[90vh] overflow-y-auto sm:max-w-xl">
            <form
              className="flex flex-col"
              onSubmit={(ev: FormEvent<HTMLFormElement>) => {
                ev.preventDefault();
                const form = new FormData(ev.currentTarget);
                if (painel.tipo === "contrato-editar") void onEditarContrato(form);
                else void onSalvarContrato(form, painel.tipo === "contrato-renovar");
              }}
            >
              <DialogHeader className="border-0 px-0 pt-0">
                <DialogTitle>
                  {painel.tipo === "contrato-editar" ? "Editar contrato" : painel.tipo === "contrato-renovar" ? "Renovar contrato" : "Incluir contrato"}
                </DialogTitle>
                <DialogDescription>
                  {painel.tipo === "contrato-renovar"
                    ? "O contrato atual será encerrado e um novo será cadastrado."
                    : "Um imóvel admite apenas um contrato ativo."}
                </DialogDescription>
              </DialogHeader>
              <div className="grid gap-4 px-6 py-2 sm:grid-cols-2">
                {painel.tipo !== "contrato-editar" ? (
                  <>
                    <Campo rotulo="Inquilino" htmlFor="inquilinoId" classe="sm:col-span-2">
                      <select id="inquilinoId" name="inquilinoId" required defaultValue={inquilino?.id ?? listaInquilinos[0]?.id} className={selectClass}>
                        {listaInquilinos.map((i) => (
                          <option key={i.id} value={i.id}>
                            {i.nome}
                          </option>
                        ))}
                      </select>
                    </Campo>
                    <Campo rotulo="Número do contrato" htmlFor="numeroContrato" classe="sm:col-span-2">
                      <Input id="numeroContrato" name="numeroContrato" required className="h-10 rounded-[4px]" />
                    </Campo>
                    <Campo rotulo="Início" htmlFor="dataInicio">
                      <Input id="dataInicio" name="dataInicio" type="date" required className="h-10 rounded-[4px]" />
                    </Campo>
                  </>
                ) : null}
                <Campo rotulo="Fim previsto" htmlFor="dataFimPrevista">
                  <Input id="dataFimPrevista" name="dataFimPrevista" type="date" defaultValue={isoDate(contratoAtivo?.dataFimPrevista)} className="h-10 rounded-[4px]" />
                </Campo>
                <Campo rotulo="Dia de vencimento" htmlFor="diaVencimento">
                  <Input id="diaVencimento" name="diaVencimento" type="number" min={1} max={31} required defaultValue={contratoAtivo?.diaVencimento ?? 10} className="h-10 rounded-[4px]" />
                </Campo>
                <Campo rotulo="Valor do aluguel" htmlFor="valorAluguel">
                  <Input id="valorAluguel" name="valorAluguel" type="number" min={0.01} step="0.01" required defaultValue={contratoAtivo?.valorAluguel ?? ""} className="h-10 rounded-[4px]" />
                </Campo>
                <Campo rotulo="Juros atraso (%)" htmlFor="jurosAtrasoPct">
                  <Input id="jurosAtrasoPct" name="jurosAtrasoPct" type="number" step="0.01" defaultValue={contratoAtivo?.jurosAtrasoPct ?? ""} className="h-10 rounded-[4px]" />
                </Campo>
                <Campo rotulo="Multa atraso (%)" htmlFor="multaAtrasoPct">
                  <Input id="multaAtrasoPct" name="multaAtrasoPct" type="number" step="0.01" defaultValue={contratoAtivo?.multaAtrasoPct ?? ""} className="h-10 rounded-[4px]" />
                </Campo>
              </div>
              <AlertaErro mensagem={erro} />
              <DialogFooter className="border-0 px-0 pb-0">
                <Button type="button" variant="secondary" className="h-9 rounded-[4px] bg-[#95A5A6] text-white hover:bg-[#7F8C8D]" onClick={fechar}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={pending} className="h-9 rounded-[4px] px-4">
                  Salvar
                </Button>
              </DialogFooter>
            </form>
          </DialogContent>
        ) : null}

        {painel?.tipo === "contrato-encerrar" ? (
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Encerrar contrato</DialogTitle>
              <DialogDescription>
                {contratoAtivo?.numeroContrato} deixa de vigorar e o imóvel pode receber um novo contrato.
              </DialogDescription>
            </DialogHeader>
            <AlertaErro mensagem={erro} />
            <DialogFooter>
              <Button type="button" variant="secondary" className="h-9 rounded-[4px] bg-[#95A5A6] text-white hover:bg-[#7F8C8D]" onClick={fechar}>
                Cancelar
              </Button>
              <Button type="button" disabled={pending} className="h-9 rounded-[4px] px-4" onClick={() => void onEncerrarContrato()}>
                Encerrar
              </Button>
            </DialogFooter>
          </DialogContent>
        ) : null}
      </Dialog>
    </div>
  );
}

function rotuloTipo(tipo: string): string {
  if (tipo === "Galpao") return "Galpão";
  return tipo;
}

function isoDate(valor: string | null | undefined): string {
  if (!valor) return "";
  return valor.slice(0, 10);
}

function textoOuNulo(valor: FormDataEntryValue | null): string | null {
  const s = String(valor ?? "").trim();
  return s.length ? s : null;
}

function numeroOuNulo(valor: FormDataEntryValue | null): number | null {
  const s = String(valor ?? "").trim().replace(",", ".");
  if (!s) return null;
  const n = Number(s);
  return Number.isFinite(n) ? n : null;
}

function Campo({ rotulo, htmlFor, children, classe }: { rotulo: string; htmlFor: string; children: ReactNode; classe?: string }) {
  return (
    <div className={cn("flex flex-col gap-1.5", classe)}>
      <Label htmlFor={htmlFor} className="text-[#333] dark:text-foreground">
        {rotulo}
      </Label>
      {children}
    </div>
  );
}

function CampoDetalhe({ rotulo, valor }: { rotulo: string; valor: ReactNode }) {
  return (
    <div>
      <dt className="text-xs font-medium tracking-wide text-muted-foreground uppercase">{rotulo}</dt>
      <dd className="mt-1 text-sm">{valor}</dd>
    </div>
  );
}

function AlertaErro({ mensagem }: { mensagem: string | null }) {
  if (!mensagem) return null;
  return (
    <p className="mx-6 mb-2 rounded-[4px] bg-[#f8d7da] px-3 py-2 text-sm text-[#721c24]" role="alert">
      {mensagem}
    </p>
  );
}

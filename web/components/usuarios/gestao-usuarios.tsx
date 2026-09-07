"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition, type ReactNode } from "react";
import { toast } from "sonner";
import { CardField, DataList, DesktopTable, MobileCard } from "@/components/data/data-list";
import { EmptyState } from "@/components/data/empty-state";
import { PageHeader } from "@/components/data/page-header";
import { SearchForm } from "@/components/data/search-form";
import { StatusBadge } from "@/components/data/status-badge";
import { Button, buttonVariants } from "@/components/ui/button";
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
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { bff, type AtualizacaoUsuarioInput, type NovoUsuarioInput } from "@/lib/api/browser";
import type { Usuario } from "@/lib/api/types";
import { formatarCpfCnpj, formatarData, formatarDataHora } from "@/lib/format";
import { cn } from "@/lib/utils";

type Painel =
  | { tipo: "criar" }
  | { tipo: "detalhe"; usuario: Usuario }
  | { tipo: "editar"; usuario: Usuario }
  | { tipo: "excluir"; usuario: Usuario };

const PERFIS = ["Gestor", "Analista"] as const;
const STATUS = ["Ativo", "Inativo", "Bloqueado"] as const;

const selectClass =
  "h-10 w-full rounded-[4px] border border-input bg-transparent px-3 text-sm outline-none focus-visible:border-primary focus-visible:ring-[3px] focus-visible:ring-primary/20 dark:bg-input/30";

export function GestaoUsuarios({
  usuarios,
  total,
  clienteId,
  clienteNome,
  termo,
}: {
  usuarios: Usuario[];
  total: number;
  clienteId: string;
  clienteNome?: string;
  termo?: string;
}) {
  const router = useRouter();
  const [painel, setPainel] = useState<Painel | null>(null);
  const [pending, startTransition] = useTransition();
  const [erro, setErro] = useState<string | null>(null);

  function fechar() {
    setPainel(null);
    setErro(null);
  }

  function abrirDetalhe(usuario: Usuario) {
    setErro(null);
    setPainel({ tipo: "detalhe", usuario });
    void bff
      .usuario(usuario.id)
      .then((atual) => setPainel((atualPainel) => (atualPainel?.tipo === "detalhe" ? { tipo: "detalhe", usuario: atual } : atualPainel)))
      .catch(() => undefined);
  }

  function recarregar() {
    startTransition(() => {
      router.refresh();
    });
  }

  async function onCriar(form: FormData) {
    setErro(null);
    const dados: NovoUsuarioInput = {
      clienteId,
      nome: String(form.get("nome") ?? "").trim(),
      email: String(form.get("email") ?? "").trim(),
      cpf: String(form.get("cpf") ?? "").trim() || null,
      telefone: String(form.get("telefone") ?? "").trim() || null,
      perfil: form.get("perfil") === "Gestor" ? "Gestor" : "Analista",
      senha: String(form.get("senha") ?? ""),
    };
    try {
      await bff.criarUsuario(dados);
      toast.success("Usuário incluído.");
      fechar();
      recarregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Não foi possível incluir o usuário.");
    }
  }

  async function onEditar(usuario: Usuario, form: FormData) {
    setErro(null);
    const dados: AtualizacaoUsuarioInput = {
      nome: String(form.get("nome") ?? "").trim(),
      telefone: String(form.get("telefone") ?? "").trim() || null,
      perfil: form.get("perfil") === "Gestor" ? "Gestor" : "Analista",
      status: statusDoForm(form.get("status")),
    };
    try {
      const atualizado = await bff.atualizarUsuario(usuario.id, dados);
      toast.success("Usuário atualizado.");
      setPainel({ tipo: "detalhe", usuario: atualizado });
      recarregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Não foi possível atualizar o usuário.");
    }
  }

  async function onRemover(usuario: Usuario) {
    setErro(null);
    try {
      await bff.removerUsuario(usuario.id);
      toast.success("Usuário removido.");
      fechar();
      recarregar();
    } catch (e) {
      setErro(e instanceof Error ? e.message : "Não foi possível remover o usuário.");
    }
  }

  return (
    <div>
      <p className="mb-3">
        <Link href="/usuarios" className="text-sm font-medium text-primary hover:text-primary/80">
          ← Clientes
        </Link>
      </p>
      <PageHeader
        titulo="Usuários"
        descricao={
          clienteNome
            ? `Usuários de ${clienteNome} (Gestor e Analista). Limite conforme o plano.`
            : "Usuários do cliente (Gestor e Analista). Limite conforme o plano."
        }
        acao={
          <Button
            type="button"
            className={cn(buttonVariants(), "h-9 rounded-[4px] px-4")}
            onClick={() => {
              setErro(null);
              setPainel({ tipo: "criar" });
            }}
          >
            Incluir usuário
          </Button>
        }
      />
      <SearchForm placeholder="Nome ou e-mail" defaultValue={termo} hidden={{ clienteId }} />

      {usuarios.length === 0 ? (
        <EmptyState mensagem="Nenhum usuário cadastrado neste cliente." />
      ) : (
        <DataList
          desktop={
            <DesktopTable>
              <Table>
                <TableHeader className="bg-[#34495e] [&_th]:text-white">
                  <TableRow className="hover:bg-transparent">
                    <TableHead className="text-white">Nome</TableHead>
                    <TableHead className="text-white">E-mail</TableHead>
                    <TableHead className="text-white">Perfil</TableHead>
                    <TableHead className="text-white">Último login</TableHead>
                    <TableHead className="text-white">Status</TableHead>
                    <TableHead className="text-white">Ações</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {usuarios.map((u) => (
                    <TableRow
                      key={u.id}
                      data-usuario-id={u.id}
                      className="cursor-pointer"
                      onClick={() => abrirDetalhe(u)}
                      onKeyDown={(ev) => {
                        if (ev.key === "Enter" || ev.key === " ") {
                          ev.preventDefault();
                          abrirDetalhe(u);
                        }
                      }}
                      tabIndex={0}
                      aria-label={`Ver detalhes de ${u.nome}`}
                    >
                      <TableCell className="font-medium">{u.nome}</TableCell>
                      <TableCell>{u.email}</TableCell>
                      <TableCell>
                        <StatusBadge status={u.perfil} />
                      </TableCell>
                      <TableCell>{formatarData(u.ultimoLogin)}</TableCell>
                      <TableCell>
                        <StatusBadge status={u.status} />
                      </TableCell>
                      <TableCell>
                        <div className="flex gap-2" onClick={(ev) => ev.stopPropagation()}>
                          <Button
                            type="button"
                            className="h-8 rounded-[4px] bg-primary px-3 text-xs text-primary-foreground hover:bg-[#C55A32]"
                            onClick={() => {
                              setErro(null);
                              setPainel({ tipo: "editar", usuario: u });
                            }}
                          >
                            Editar
                          </Button>
                          <Button
                            type="button"
                            className="h-8 rounded-[4px] bg-[#E74C3C] px-3 text-xs text-white hover:bg-[#C0392B]"
                            onClick={() => {
                              setErro(null);
                              setPainel({ tipo: "excluir", usuario: u });
                            }}
                          >
                            Remover
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </DesktopTable>
          }
          mobile={usuarios.map((u) => (
            <MobileCard key={u.id}>
              <button type="button" className="mb-2 w-full text-left font-semibold" onClick={() => abrirDetalhe(u)}>
                {u.nome}
              </button>
              <CardField label="E-mail">{u.email}</CardField>
              <CardField label="Perfil">
                <StatusBadge status={u.perfil} />
              </CardField>
              <CardField label="Status">
                <StatusBadge status={u.status} />
              </CardField>
              <div className="mt-3 flex gap-2">
                <Button
                  type="button"
                  className="h-8 flex-1 rounded-[4px] bg-primary text-xs text-primary-foreground hover:bg-[#C55A32]"
                  onClick={() => {
                    setErro(null);
                    setPainel({ tipo: "editar", usuario: u });
                  }}
                >
                  Editar
                </Button>
                <Button
                  type="button"
                  className="h-8 flex-1 rounded-[4px] bg-[#E74C3C] text-xs text-white hover:bg-[#C0392B]"
                  onClick={() => {
                    setErro(null);
                    setPainel({ tipo: "excluir", usuario: u });
                  }}
                >
                  Remover
                </Button>
              </div>
            </MobileCard>
          ))}
        />
      )}
      <p className="mt-3 text-xs text-muted-foreground">{total} usuário(s)</p>

      <Dialog open={painel !== null} onOpenChange={(aberto) => { if (!aberto) fechar(); }}>
        {painel?.tipo === "criar" ? (
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Incluir usuário</DialogTitle>
              <DialogDescription>Cria um Gestor ou Analista para este cliente.</DialogDescription>
            </DialogHeader>
            <form
              className="flex flex-col gap-4 px-6 py-4"
              onSubmit={(ev) => {
                ev.preventDefault();
                void onCriar(new FormData(ev.currentTarget));
              }}
            >
              <CamposUsuario modo="criar" />
              <AlertaErro mensagem={erro} />
              <DialogFooter className="border-0 px-0 pb-0">
                <Button type="button" variant="secondary" className="h-9 rounded-[4px] bg-[#95A5A6] text-white hover:bg-[#7F8C8D]" onClick={fechar}>
                  Cancelar
                </Button>
                <Button type="submit" disabled={pending} className="h-9 rounded-[4px] px-4">
                  Cadastrar
                </Button>
              </DialogFooter>
            </form>
          </DialogContent>
        ) : null}

        {painel?.tipo === "detalhe" ? (
          <DialogContent>
            <DialogHeader>
              <DialogTitle>{painel.usuario.nome}</DialogTitle>
              <DialogDescription>Detalhes do usuário do cliente.</DialogDescription>
            </DialogHeader>
            <dl className="grid gap-3 px-6 py-4 sm:grid-cols-2">
              <CampoDetalhe rotulo="Nome" valor={painel.usuario.nome} />
              <CampoDetalhe rotulo="E-mail" valor={painel.usuario.email} />
              <CampoDetalhe rotulo="CPF" valor={formatarCpfCnpj(painel.usuario.cpf)} />
              <CampoDetalhe rotulo="Telefone" valor={painel.usuario.telefone ?? "—"} />
              <CampoDetalhe rotulo="Perfil" valor={<StatusBadge status={painel.usuario.perfil} />} />
              <CampoDetalhe rotulo="Status" valor={<StatusBadge status={painel.usuario.status} />} />
              <CampoDetalhe rotulo="Último login" valor={formatarDataHora(painel.usuario.ultimoLogin)} />
            </dl>
            <DialogFooter>
              <Button
                type="button"
                className="h-9 rounded-[4px] bg-[#E74C3C] px-4 text-white hover:bg-[#C0392B]"
                onClick={() => setPainel({ tipo: "excluir", usuario: painel.usuario })}
              >
                Remover
              </Button>
              <Button
                type="button"
                className="h-9 rounded-[4px] px-4"
                onClick={() => {
                  setErro(null);
                  setPainel({ tipo: "editar", usuario: painel.usuario });
                }}
              >
                Editar
              </Button>
            </DialogFooter>
          </DialogContent>
        ) : null}

        {painel?.tipo === "editar" ? (
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Editar usuário</DialogTitle>
              <DialogDescription>E-mail e CPF não podem ser alterados.</DialogDescription>
            </DialogHeader>
            <form
              className="flex flex-col gap-4 px-6 py-4"
              onSubmit={(ev) => {
                ev.preventDefault();
                void onEditar(painel.usuario, new FormData(ev.currentTarget));
              }}
            >
              <CamposUsuario modo="editar" usuario={painel.usuario} />
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

        {painel?.tipo === "excluir" ? (
          <DialogContent>
            <DialogHeader>
              <DialogTitle>Remover usuário</DialogTitle>
              <DialogDescription>
                {painel.usuario.nome} será desativado (não poderá mais entrar). Essa ação respeita o soft
                delete do cadastro.
              </DialogDescription>
            </DialogHeader>
            <div className="px-6 py-2">
              <AlertaErro mensagem={erro} />
            </div>
            <DialogFooter>
              <Button type="button" variant="secondary" className="h-9 rounded-[4px] bg-[#95A5A6] text-white hover:bg-[#7F8C8D]" onClick={fechar}>
                Cancelar
              </Button>
              <Button
                type="button"
                disabled={pending}
                className="h-9 rounded-[4px] bg-[#E74C3C] px-4 text-white hover:bg-[#C0392B]"
                onClick={() => void onRemover(painel.usuario)}
              >
                Remover
              </Button>
            </DialogFooter>
          </DialogContent>
        ) : null}
      </Dialog>
    </div>
  );
}

function statusDoForm(valor: FormDataEntryValue | null): AtualizacaoUsuarioInput["status"] {
  if (valor === "Inativo" || valor === "Bloqueado") return valor;
  return "Ativo";
}

function CamposUsuario({ modo, usuario }: { modo: "criar" | "editar"; usuario?: Usuario }) {
  return (
    <>
      <div className="grid gap-4 sm:grid-cols-2">
        <Campo rotulo="Nome" htmlFor="nome">
          <Input id="nome" name="nome" required defaultValue={usuario?.nome} className="h-10 rounded-[4px]" />
        </Campo>
        <Campo rotulo="E-mail" htmlFor="email">
          <Input
            id="email"
            name="email"
            type="email"
            required={modo === "criar"}
            defaultValue={usuario?.email}
            disabled={modo === "editar"}
            className="h-10 rounded-[4px]"
          />
        </Campo>
        <Campo rotulo="CPF" htmlFor="cpf">
          <Input
            id="cpf"
            name="cpf"
            defaultValue={usuario?.cpf ?? ""}
            disabled={modo === "editar"}
            className="h-10 rounded-[4px]"
            placeholder="Opcional"
          />
        </Campo>
        <Campo rotulo="Telefone" htmlFor="telefone">
          <Input id="telefone" name="telefone" defaultValue={usuario?.telefone ?? ""} className="h-10 rounded-[4px]" />
        </Campo>
        <Campo rotulo="Perfil" htmlFor="perfil">
          <select id="perfil" name="perfil" defaultValue={usuario?.perfil === "Gestor" ? "Gestor" : "Analista"} className={selectClass}>
            {PERFIS.map((p) => (
              <option key={p} value={p}>
                {p}
              </option>
            ))}
          </select>
        </Campo>
        {modo === "editar" ? (
          <Campo rotulo="Status" htmlFor="status">
            <select id="status" name="status" defaultValue={usuario?.status ?? "Ativo"} className={selectClass}>
              {STATUS.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </select>
          </Campo>
        ) : (
          <Campo rotulo="Senha" htmlFor="senha">
            <Input id="senha" name="senha" type="password" required minLength={10} autoComplete="new-password" className="h-10 rounded-[4px]" />
            <p className="mt-1 text-xs text-muted-foreground">
              Mínimo 10 caracteres, com maiúscula, minúscula, número e símbolo.
            </p>
          </Campo>
        )}
      </div>
    </>
  );
}

function Campo({ rotulo, htmlFor, children }: { rotulo: string; htmlFor: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-1.5">
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
    <p className="rounded-[4px] bg-[#f8d7da] px-3 py-2 text-sm text-[#721c24]" role="alert">
      {mensagem}
    </p>
  );
}

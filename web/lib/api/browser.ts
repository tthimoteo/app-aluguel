import type { Usuario } from "@/lib/api/types";
import { mensagemApiErro } from "@/lib/api/erro";

export class BffError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
    this.name = "BffError";
  }
}

async function bffFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
    cache: "no-store",
  });

  if (res.status === 204) {
    return undefined as T;
  }

  const texto = await res.text();
  const body = texto ? (JSON.parse(texto) as unknown) : null;

  if (!res.ok) {
    throw new BffError(res.status, mensagemApiErro(body));
  }

  return body as T;
}

export type UsuarioPorCpf = {
  id: string;
  nome: string;
  email: string;
  telefone: string | null;
  cpf: string;
  jaNoCliente: boolean;
};

export type NovoUsuarioInput = {
  clienteId: string;
  nome: string;
  email: string;
  cpf?: string | null;
  telefone?: string | null;
  perfil: "Gestor" | "Analista";
  senha?: string | null;
};

export type AtualizacaoUsuarioInput = {
  nome: string;
  email: string;
  telefone?: string | null;
  perfil: "Gestor" | "Analista";
  status: "Ativo" | "Inativo" | "Bloqueado";
};

export const bff = {
  usuario: (id: string, clienteId?: string) =>
    bffFetch<Usuario>(`/api/usuarios/${id}${clienteId ? `?clienteId=${encodeURIComponent(clienteId)}` : ""}`),
  usuarioPorCpf: (clienteId: string, cpf: string) =>
    bffFetch<UsuarioPorCpf | null>(
      `/api/usuarios/por-cpf?clienteId=${encodeURIComponent(clienteId)}&cpf=${encodeURIComponent(cpf)}`,
    ),
  criarUsuario: (dados: NovoUsuarioInput) =>
    bffFetch<Usuario>("/api/usuarios", { method: "POST", body: JSON.stringify(dados) }),
  atualizarUsuario: (id: string, dados: AtualizacaoUsuarioInput, clienteId?: string) =>
    bffFetch<Usuario>(
      `/api/usuarios/${id}${clienteId ? `?clienteId=${encodeURIComponent(clienteId)}` : ""}`,
      { method: "PUT", body: JSON.stringify(dados) },
    ),
  removerUsuario: (id: string, clienteId?: string) =>
    bffFetch<void>(
      `/api/usuarios/${id}${clienteId ? `?clienteId=${encodeURIComponent(clienteId)}` : ""}`,
      { method: "DELETE" },
    ),
  selecionarCliente: (clienteId: string) =>
    bffFetch<{ usuario: unknown }>("/api/auth/contexto", {
      method: "POST",
      body: JSON.stringify({ clienteId }),
    }),
};

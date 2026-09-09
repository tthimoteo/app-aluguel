import type { Contrato, Imovel, Inquilino, Usuario } from "@/lib/api/types";
import type { EnderecoViaCep } from "@/lib/cep";
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

export type NovoImovelInput = {
  clienteId: string;
  nome: string;
  tipo: "Residencial" | "Comercial" | "Galpao" | "Sala" | "Outro";
  numeroIptu?: string | null;
  numeroMatricula?: string | null;
  endereco?: {
    logradouro?: string | null;
    numero?: string | null;
    complemento?: string | null;
    bairro?: string | null;
    cidade?: string | null;
    uf?: string | null;
    cep?: string | null;
  } | null;
};

export type AtualizacaoImovelInput = {
  nome: string;
  tipo: NovoImovelInput["tipo"];
  numeroIptu?: string | null;
  numeroMatricula?: string | null;
  status: "Ativo" | "Inativo";
  endereco?: NovoImovelInput["endereco"];
};

export type NovoInquilinoInput = {
  clienteId: string;
  tipoPessoa: "PF" | "PJ";
  nome: string;
  documento: string;
  inscricaoMunicipal?: string | null;
  telefone?: string | null;
  email?: string | null;
  endereco?: NovoImovelInput["endereco"];
};

export type AtualizacaoInquilinoInput = {
  nome: string;
  inscricaoMunicipal?: string | null;
  telefone?: string | null;
  email?: string | null;
  status: "Ativo" | "Inativo";
  endereco?: NovoImovelInput["endereco"];
};

export type NovoContratoInput = {
  imovelId: string;
  inquilinoId: string;
  numeroContrato: string;
  dataInicio: string;
  dataFimPrevista?: string | null;
  diaVencimento: number;
  valorAluguel: number;
  jurosAtrasoPct?: number | null;
  multaAtrasoPct?: number | null;
};

export type AtualizacaoContratoInput = {
  dataFimPrevista?: string | null;
  diaVencimento: number;
  valorAluguel: number;
  jurosAtrasoPct?: number | null;
  multaAtrasoPct?: number | null;
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
  criarImovel: (dados: NovoImovelInput) =>
    bffFetch<Imovel>("/api/imoveis", { method: "POST", body: JSON.stringify(dados) }),
  atualizarImovel: (id: string, dados: AtualizacaoImovelInput) =>
    bffFetch<Imovel>(`/api/imoveis/${id}`, { method: "PUT", body: JSON.stringify(dados) }),
  consultarCep: (cep: string) => bffFetch<EnderecoViaCep>(`/api/cep/${encodeURIComponent(cep)}`),
  criarInquilino: (dados: NovoInquilinoInput) =>
    bffFetch<Inquilino>("/api/inquilinos", { method: "POST", body: JSON.stringify(dados) }),
  atualizarInquilino: (id: string, dados: AtualizacaoInquilinoInput) =>
    bffFetch<Inquilino>(`/api/inquilinos/${id}`, { method: "PUT", body: JSON.stringify(dados) }),
  removerInquilino: (id: string) =>
    bffFetch<void>(`/api/inquilinos/${id}`, { method: "DELETE" }),
  criarContrato: (dados: NovoContratoInput) =>
    bffFetch<Contrato>("/api/contratos", { method: "POST", body: JSON.stringify(dados) }),
  atualizarContrato: (id: string, dados: AtualizacaoContratoInput) =>
    bffFetch<Contrato>(`/api/contratos/${id}`, { method: "PUT", body: JSON.stringify(dados) }),
  encerrarContrato: (id: string) =>
    bffFetch<Contrato>(`/api/contratos/${id}/encerrar`, { method: "POST" }),
  selecionarCliente: (clienteId: string) =>
    bffFetch<{ usuario: unknown }>("/api/auth/contexto", {
      method: "POST",
      body: JSON.stringify({ clienteId }),
    }),
};

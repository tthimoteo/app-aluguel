import { redirect } from "next/navigation";
import { getApiUrl } from "@/lib/api/config";
import type {
  AuthMe,
  CertificadoDigital,
  Cliente,
  CobrancaDetalhe,
  CobrancaListItem,
  CobrancaResumo,
  Contrato,
  Imovel,
  Inquilino,
  IntegracaoBoletoCliente,
  Pagina,
  Plano,
  ProvedorBoleto,
  Usuario,
} from "@/lib/api/types";
import { getAccessTokenFromJar } from "@/lib/auth/cookies";

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly body: unknown,
  ) {
    const detalhe =
      typeof body === "object" && body !== null && "detail" in body
        ? String((body as { detail: unknown }).detail)
        : `Falha na API (${status})`;
    super(detalhe);
    this.name = "ApiError";
  }
}

async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const token = await getAccessTokenFromJar();
  if (!token) redirect("/login");

  const res = await fetch(`${getApiUrl()}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      Authorization: `Bearer ${token}`,
      ...init?.headers,
    },
    cache: "no-store",
  });

  if (res.status === 401) {
    redirect("/login");
  }

  if (res.status === 204) {
    return undefined as T;
  }

  const texto = await res.text();
  const body = texto ? (JSON.parse(texto) as unknown) : null;

  if (!res.ok) {
    throw new ApiError(res.status, body);
  }

  return body as T;
}

function qs(params: Record<string, string | number | undefined | null>): string {
  const search = new URLSearchParams();
  for (const [k, v] of Object.entries(params)) {
    if (v === undefined || v === null || v === "") continue;
    search.set(k, String(v));
  }
  const s = search.toString();
  return s ? `?${s}` : "";
}

export const api = {
  me: () => apiFetch<AuthMe>("/api/auth/me"),
  planos: () => apiFetch<Plano[]>("/api/planos"),
  clientes: (opts?: { termo?: string; take?: number }) =>
    apiFetch<Pagina<Cliente>>(`/api/clientes${qs({ termo: opts?.termo, take: opts?.take ?? 20 })}`),
  imoveis: (opts?: { termo?: string; take?: number; clienteId?: string }) =>
    apiFetch<Pagina<Imovel>>(
      `/api/imoveis${qs({ termo: opts?.termo, take: opts?.take ?? 20, clienteId: opts?.clienteId })}`,
    ),
  inquilinos: (opts?: { termo?: string; take?: number; clienteId?: string; imovelId?: string }) =>
    apiFetch<Pagina<Inquilino>>(
      `/api/inquilinos${qs({ termo: opts?.termo, take: opts?.take ?? 20, clienteId: opts?.clienteId, imovelId: opts?.imovelId })}`,
    ),
  inquilino: (id: string) => apiFetch<Inquilino>(`/api/inquilinos/${id}`),
  imovel: (id: string) => apiFetch<Imovel>(`/api/imoveis/${id}`),
  contratos: (opts?: { take?: number; clienteId?: string; imovelId?: string; status?: string }) =>
    apiFetch<Pagina<Contrato>>(
      `/api/contratos${qs({
        take: opts?.take ?? 20,
        clienteId: opts?.clienteId,
        imovelId: opts?.imovelId,
        status: opts?.status,
      })}`,
    ),
  usuarios: (opts?: { termo?: string; take?: number; clienteId?: string }) =>
    apiFetch<Pagina<Usuario>>(
      `/api/usuarios${qs({ termo: opts?.termo, take: opts?.take ?? 20, clienteId: opts?.clienteId })}`,
    ),
  usuario: (id: string, clienteId?: string) =>
    apiFetch<Usuario>(`/api/usuarios/${id}${qs({ clienteId })}`),
  cliente: (id: string) => apiFetch<Cliente>(`/api/clientes/${id}`),
  certificadoCliente: async (clienteId: string) => {
    try {
      return await apiFetch<CertificadoDigital>(`/api/clientes/${clienteId}/certificado`);
    } catch (e) {
      if (e instanceof ApiError && e.status === 404) return null;
      throw e;
    }
  },
  boletoProvedores: () => apiFetch<ProvedorBoleto[]>("/api/boleto-provedores"),
  integracaoBoleto: async (clienteId: string) => {
    try {
      return await apiFetch<IntegracaoBoletoCliente>(`/api/clientes/${clienteId}/integracao-boleto`);
    } catch (e) {
      if (e instanceof ApiError && e.status === 404) return null;
      throw e;
    }
  },
  cobrancas: (opts?: {
    clienteId?: string;
    imovelId?: string;
    competencia?: string;
    take?: number;
    skip?: number;
  }) =>
    apiFetch<Pagina<CobrancaListItem>>(
      `/api/cobrancas${qs({
        clienteId: opts?.clienteId,
        imovelId: opts?.imovelId,
        competencia: opts?.competencia,
        take: opts?.take ?? 50,
        skip: opts?.skip ?? 0,
      })}`,
    ),
  cobrancaResumo: (opts?: { clienteId?: string }) =>
    apiFetch<CobrancaResumo>(`/api/cobrancas/resumo${qs({ clienteId: opts?.clienteId })}`),
  cobranca: (id: string) => apiFetch<CobrancaDetalhe>(`/api/cobrancas/${id}`),
};

import { getApiUrl } from "@/lib/api/config";
import type { Plano } from "@/lib/api/types";

/** Catálogo de planos sem autenticação (endpoint público da API). */
export async function listarPlanosPublicos(): Promise<Plano[]> {
  try {
    const res = await fetch(`${getApiUrl()}/api/planos`, {
      headers: { Accept: "application/json" },
      next: { revalidate: 60 },
    });
    if (!res.ok) return [];
    return (await res.json()) as Plano[];
  } catch {
    return [];
  }
}

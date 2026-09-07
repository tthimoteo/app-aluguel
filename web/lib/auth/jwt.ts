import type { UsuarioAutenticado } from "@/lib/api/types";

function decodeBase64Url(input: string): string {
  const padded = input.replace(/-/g, "+").replace(/_/g, "/");
  if (typeof atob === "function") {
    return atob(padded);
  }
  return Buffer.from(input, "base64url").toString("utf8");
}

/** Decodifica o payload do JWT (sem verificar a assinatura — a API .NET valida). */
export function decodeAccessToken(token: string): UsuarioAutenticado | null {
  try {
    const part = token.split(".")[1];
    if (!part) return null;
    const payload = JSON.parse(decodeBase64Url(part)) as Record<string, unknown>;
    const role = payload.role;
    const roles = Array.isArray(role)
      ? role.filter((r): r is string => typeof r === "string")
      : typeof role === "string"
        ? [role]
        : [];

    const id = typeof payload.sub === "string" ? payload.sub : null;
    const email = typeof payload.email === "string" ? payload.email : "";
    const nome = typeof payload.name === "string" ? payload.name : email;
    const tenantId = typeof payload.tenant_id === "string" ? payload.tenant_id : "";
    if (!id || !tenantId) return null;

    return {
      id,
      email,
      nome,
      tenantId,
      clienteId: typeof payload.cliente_id === "string" ? payload.cliente_id : null,
      roles,
    };
  } catch {
    return null;
  }
}

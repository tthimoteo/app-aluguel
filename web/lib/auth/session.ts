import { redirect } from "next/navigation";
import { getAccessTokenFromJar } from "@/lib/auth/cookies";
import { decodeAccessToken } from "@/lib/auth/jwt";
import type { UsuarioAutenticado } from "@/lib/api/types";

export async function getSession(): Promise<UsuarioAutenticado | null> {
  const token = await getAccessTokenFromJar();
  if (!token) return null;
  return decodeAccessToken(token);
}

export async function requireSession(): Promise<UsuarioAutenticado> {
  const session = await getSession();
  if (!session) redirect("/login");
  return session;
}

export function temPerfil(usuario: UsuarioAutenticado, ...perfis: string[]): boolean {
  return usuario.roles.some((r) => perfis.includes(r));
}

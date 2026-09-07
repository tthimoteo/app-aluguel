import { NextResponse } from "next/server";
import { getApiUrl } from "@/lib/api/config";
import type { TokensAutenticacao } from "@/lib/api/types";
import { setAuthCookiesOnJar } from "@/lib/auth/cookies";

export async function POST(request: Request) {
  let body: { email?: string; senha?: string };
  try {
    body = (await request.json()) as { email?: string; senha?: string };
  } catch {
    return NextResponse.json({ erro: "JSON inválido." }, { status: 400 });
  }

  const email = body.email?.trim() ?? "";
  const senha = body.senha ?? "";
  if (!email || !senha) {
    return NextResponse.json({ erro: "E-mail e senha são obrigatórios." }, { status: 400 });
  }

  const res = await fetch(`${getApiUrl()}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify({ email, senha }),
    cache: "no-store",
  });

  if (res.status === 401) {
    return NextResponse.json({ erro: "E-mail ou senha inválidos." }, { status: 401 });
  }

  if (!res.ok) {
    return NextResponse.json({ erro: "Não foi possível entrar. Tente novamente." }, { status: 502 });
  }

  const tokens = (await res.json()) as TokensAutenticacao;
  await setAuthCookiesOnJar(tokens);

  return NextResponse.json({
    usuario: tokens.usuario,
    accessTokenExpiresAtUtc: tokens.accessTokenExpiresAtUtc,
  });
}

import { NextResponse } from "next/server";
import { getApiUrl } from "@/lib/api/config";
import type { TokensAutenticacao } from "@/lib/api/types";
import { getRefreshTokenFromJar, setAuthCookiesOnJar } from "@/lib/auth/cookies";

export async function POST() {
  const refresh = await getRefreshTokenFromJar();
  if (!refresh) {
    return NextResponse.json({ erro: "Sessão expirada." }, { status: 401 });
  }

  const res = await fetch(`${getApiUrl()}/api/auth/refresh`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify({ refreshToken: refresh }),
    cache: "no-store",
  });

  if (!res.ok) {
    return NextResponse.json({ erro: "Sessão expirada." }, { status: 401 });
  }

  const tokens = (await res.json()) as TokensAutenticacao;
  await setAuthCookiesOnJar(tokens);
  return NextResponse.json({ ok: true });
}

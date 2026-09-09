import { NextResponse } from "next/server";
import { getApiUrl } from "@/lib/api/config";
import type { TokensAutenticacao } from "@/lib/api/types";
import { getAccessTokenFromJar, getRefreshTokenFromJar, setAuthCookiesOnJar } from "@/lib/auth/cookies";

export async function POST(request: Request) {
  let body: { clienteId?: string };
  try {
    body = (await request.json()) as { clienteId?: string };
  } catch {
    return NextResponse.json({ erro: "JSON inválido." }, { status: 400 });
  }

  const clienteId = body.clienteId?.trim() ?? "";
  const refresh = await getRefreshTokenFromJar();
  const access = await getAccessTokenFromJar();
  if (!clienteId || !refresh || !access) {
    return NextResponse.json({ erro: "Não autenticado." }, { status: 401 });
  }

  const res = await fetch(`${getApiUrl()}/api/auth/contexto`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      Authorization: `Bearer ${access}`,
    },
    body: JSON.stringify({ clienteId, refreshToken: refresh }),
    cache: "no-store",
  });

  const texto = await res.text();
  if (!res.ok) {
    return NextResponse.json(texto ? JSON.parse(texto) : { erro: "Não foi possível trocar de cliente." }, {
      status: res.status,
    });
  }

  const tokens = JSON.parse(texto) as TokensAutenticacao;
  await setAuthCookiesOnJar(tokens);
  return NextResponse.json({ usuario: tokens.usuario });
}

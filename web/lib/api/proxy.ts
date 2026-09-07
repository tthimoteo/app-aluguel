import { NextResponse } from "next/server";
import { getApiUrl } from "@/lib/api/config";
import { getAccessTokenFromJar } from "@/lib/auth/cookies";

/** Encaminha a requisição autenticada para a API .NET (cookies httpOnly → Bearer). */
export async function proxyToApi(path: string, init?: RequestInit): Promise<NextResponse> {
  const token = await getAccessTokenFromJar();
  if (!token) {
    return NextResponse.json({ erro: "Não autenticado." }, { status: 401 });
  }

  const res = await fetch(`${getApiUrl()}${path}`, {
    ...init,
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
    cache: "no-store",
  });

  if (res.status === 204) {
    return new NextResponse(null, { status: 204 });
  }

  const texto = await res.text();
  return new NextResponse(texto || null, {
    status: res.status,
    headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
  });
}

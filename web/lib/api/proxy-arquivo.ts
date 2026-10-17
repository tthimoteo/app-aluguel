import { NextResponse } from "next/server";
import { getApiUrl } from "@/lib/api/config";
import { getAccessTokenFromJar } from "@/lib/auth/cookies";

/** Proxy de download binário (PDF/XML), preservando Content-Type e Content-Disposition. */
export async function proxyArquivoApi(path: string): Promise<NextResponse> {
  const token = await getAccessTokenFromJar();
  if (!token) {
    return NextResponse.json({ erro: "Não autenticado." }, { status: 401 });
  }

  const res = await fetch(`${getApiUrl()}${path}`, {
    headers: { Authorization: `Bearer ${token}` },
    cache: "no-store",
  });

  if (!res.ok) {
    const texto = await res.text();
    return new NextResponse(texto || null, {
      status: res.status,
      headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
    });
  }

  const buffer = await res.arrayBuffer();
  const headers = new Headers();
  headers.set("Content-Type", res.headers.get("Content-Type") ?? "application/octet-stream");
  const disposition = res.headers.get("Content-Disposition");
  if (disposition) headers.set("Content-Disposition", disposition);

  return new NextResponse(buffer, { status: res.status, headers });
}

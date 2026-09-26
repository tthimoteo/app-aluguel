import { proxyToApi } from "@/lib/api/proxy";
import { getApiUrl } from "@/lib/api/config";
import { getAccessTokenFromJar } from "@/lib/auth/cookies";
import { NextResponse } from "next/server";

export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return proxyToApi(`/api/clientes/${id}/certificado`);
}

/** Encaminha multipart (arquivo + senha) sem forçar Content-Type JSON. */
export async function POST(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const token = await getAccessTokenFromJar();
  if (!token) {
    return NextResponse.json({ erro: "Não autenticado." }, { status: 401 });
  }

  const form = await request.formData();
  const res = await fetch(`${getApiUrl()}/api/clientes/${id}/certificado`, {
    method: "POST",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: form,
    cache: "no-store",
  });

  const texto = await res.text();
  return new NextResponse(texto || null, {
    status: res.status,
    headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
  });
}

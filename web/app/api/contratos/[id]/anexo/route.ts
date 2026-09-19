import { NextResponse } from "next/server";
import { getApiUrl } from "@/lib/api/config";
import { getAccessTokenFromJar } from "@/lib/auth/cookies";

/** Encaminha multipart (anexo PDF) para a API .NET sem forçar Content-Type JSON. */
export async function POST(request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const token = await getAccessTokenFromJar();
  if (!token) {
    return NextResponse.json({ erro: "Não autenticado." }, { status: 401 });
  }

  const contentType = request.headers.get("content-type") ?? "";
  const body = await request.arrayBuffer();

  const res = await fetch(`${getApiUrl()}/api/contratos/${id}/anexo`, {
    method: "POST",
    headers: {
      Accept: "application/json",
      Authorization: `Bearer ${token}`,
      "Content-Type": contentType,
    },
    body,
    cache: "no-store",
  });

  const texto = await res.text();
  return new NextResponse(texto || null, {
    status: res.status,
    headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
  });
}

/** Download do PDF do contrato (stream da API .NET). */
export async function GET(_request: Request, { params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  const token = await getAccessTokenFromJar();
  if (!token) {
    return NextResponse.json({ erro: "Não autenticado." }, { status: 401 });
  }

  const res = await fetch(`${getApiUrl()}/api/contratos/${id}/anexo`, {
    method: "GET",
    headers: {
      Authorization: `Bearer ${token}`,
    },
    cache: "no-store",
  });

  if (!res.ok) {
    const texto = await res.text();
    return new NextResponse(texto || null, {
      status: res.status,
      headers: { "Content-Type": res.headers.get("Content-Type") ?? "application/json" },
    });
  }

  const headers = new Headers();
  const contentType = res.headers.get("Content-Type");
  const disposition = res.headers.get("Content-Disposition");
  if (contentType) headers.set("Content-Type", contentType);
  if (disposition) headers.set("Content-Disposition", disposition);
  headers.set("Cache-Control", "private, no-store");

  return new NextResponse(res.body, { status: 200, headers });
}

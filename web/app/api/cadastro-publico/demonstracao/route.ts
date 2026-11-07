import { NextResponse } from "next/server";
import { getApiUrl } from "@/lib/api/config";

export async function POST(request: Request) {
  let body: unknown;
  try {
    body = await request.json();
  } catch {
    return NextResponse.json({ erro: "JSON inválido." }, { status: 400 });
  }

  const res = await fetch(`${getApiUrl()}/api/cadastro-publico/demonstracao`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify(body),
    cache: "no-store",
  });

  const texto = await res.text();
  const parsed = texto ? (JSON.parse(texto) as unknown) : null;
  if (!res.ok) {
    const detalhe =
      typeof parsed === "object" && parsed && "detail" in parsed
        ? String((parsed as { detail: unknown }).detail)
        : "Não foi possível enviar a solicitação.";
    return NextResponse.json({ erro: detalhe }, { status: res.status });
  }

  return NextResponse.json(parsed);
}

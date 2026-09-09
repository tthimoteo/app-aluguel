import { NextResponse } from "next/server";
import { getAccessTokenFromJar } from "@/lib/auth/cookies";
import { parseRespostaCep, somenteDigitosCep } from "@/lib/cep";

export async function GET(_request: Request, { params }: { params: Promise<{ cep: string }> }) {
  const token = await getAccessTokenFromJar();
  if (!token) {
    return NextResponse.json({ erro: "Não autenticado." }, { status: 401 });
  }

  const { cep: bruto } = await params;
  const cep = somenteDigitosCep(bruto);
  if (cep.length !== 8) {
    return NextResponse.json({ erro: "CEP deve conter 8 dígitos." }, { status: 400 });
  }

  const res = await fetch(`https://viacep.com.br/ws/${cep}/json/`, { cache: "no-store" });
  if (!res.ok) {
    return NextResponse.json({ erro: "Não foi possível consultar o CEP." }, { status: 502 });
  }

  const body: unknown = await res.json();
  const endereco = parseRespostaCep(body);
  if (!endereco) {
    return NextResponse.json({ erro: "CEP não encontrado." }, { status: 404 });
  }

  return NextResponse.json(endereco);
}

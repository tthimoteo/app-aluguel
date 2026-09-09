import { NextResponse } from "next/server";
import { getAccessTokenFromJar } from "@/lib/auth/cookies";
import { buscarEnderecoPorCep } from "@/lib/cep";

export async function GET(_request: Request, { params }: { params: Promise<{ cep: string }> }) {
  const token = await getAccessTokenFromJar();
  if (!token) {
    return NextResponse.json({ erro: "Não autenticado." }, { status: 401 });
  }

  const { cep: bruto } = await params;
  try {
    const endereco = await buscarEnderecoPorCep(bruto);
    return NextResponse.json(endereco);
  } catch (e) {
    const mensagem = e instanceof Error ? e.message : "Não foi possível consultar o CEP.";
    if (mensagem.includes("8 dígitos")) {
      return NextResponse.json({ erro: mensagem }, { status: 400 });
    }
    if (mensagem === "CEP não encontrado.") {
      return NextResponse.json({ erro: mensagem }, { status: 404 });
    }
    return NextResponse.json({ erro: mensagem }, { status: 502 });
  }
}

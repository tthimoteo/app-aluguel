import { NextResponse } from "next/server";
import { getApiUrl } from "@/lib/api/config";
import type { TokensAutenticacao } from "@/lib/api/types";
import { setAuthCookiesOnJar } from "@/lib/auth/cookies";

type CadastroBody = {
  nome?: string;
  email?: string;
  telefone?: string;
  cpf?: string;
  senha?: string;
  planoId?: string | null;
};

type CadastroResposta = {
  clienteId: string;
  assinaturaId: string;
  statusAssinatura: string;
  operacao: string;
  mensagem: string;
  pagamentoId: string | null;
  pagamento: unknown;
  tokens: TokensAutenticacao;
};

export async function POST(request: Request) {
  let body: CadastroBody;
  try {
    body = (await request.json()) as CadastroBody;
  } catch {
    return NextResponse.json({ erro: "JSON inválido." }, { status: 400 });
  }

  const res = await fetch(`${getApiUrl()}/api/cadastro-publico`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify({
      nome: body.nome,
      email: body.email,
      telefone: body.telefone,
      cpf: body.cpf,
      senha: body.senha,
      planoId: body.planoId || null,
    }),
    cache: "no-store",
  });

  const texto = await res.text();
  const parsed = texto ? (JSON.parse(texto) as unknown) : null;

  if (!res.ok) {
    const detalhe =
      typeof parsed === "object" && parsed && "detail" in parsed
        ? String((parsed as { detail: unknown }).detail)
        : "Não foi possível concluir o cadastro.";
    return NextResponse.json({ erro: detalhe }, { status: res.status });
  }

  const data = parsed as CadastroResposta;
  await setAuthCookiesOnJar(data.tokens);

  return NextResponse.json({
    clienteId: data.clienteId,
    assinaturaId: data.assinaturaId,
    statusAssinatura: data.statusAssinatura,
    operacao: data.operacao,
    mensagem: data.mensagem,
    pagamentoId: data.pagamentoId,
    pagamento: data.pagamento,
    usuario: data.tokens.usuario,
  });
}

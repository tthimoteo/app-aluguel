import type { Plano } from "@/lib/api/types";
import { formatarMoeda } from "@/lib/format";

const ORDEM: Record<string, number> = {
  TRIAL: 0,
  BASICO: 1,
  INTERMEDIARIO: 2,
  AVANCADO: 3,
  PRO: 4,
};

export function ordemPlano(codigo: string | null | undefined): number {
  if (!codigo) return 0;
  return ORDEM[codigo.toUpperCase()] ?? 0;
}

export function tipoMudancaPlano(
  atual: Plano | null,
  destino: Plano,
): "contratacao" | "upgrade" | "downgrade" | "mesmo" {
  if (!atual || atual.codigo === "TRIAL") return "contratacao";
  const a = ordemPlano(atual.codigo);
  const b = ordemPlano(destino.codigo);
  if (b > a) return "upgrade";
  if (b < a) return "downgrade";
  return "mesmo";
}

/** Serviços/benefícios exibidos no modal de troca de plano (especificação §3). */
export function beneficiosDoPlano(plano: Plano): string[] {
  const imoveis = plano.maxImoveis == null ? "Imóveis ilimitados (sob consulta)" : `Até ${plano.maxImoveis} imóveis`;
  const usuarios = plano.maxUsuarios == null ? "Usuários sob consulta" : `Até ${plano.maxUsuarios} usuário(s)`;
  const itens = [
    imoveis,
    usuarios,
    plano.permiteNfse
      ? "Emissão de NFS-e via API Nacional"
      : "Sem emissão de NFS-e neste plano",
    plano.permiteBoleto
      ? "Emissão de boleto bancário de cobrança"
      : "Sem emissão de boleto neste plano",
    "Gestão de inquilinos e contratos",
    "Dashboard e indicadores operacionais",
  ];

  if (plano.codigo === "TRIAL") {
    itens.push(`Período de avaliação de ${plano.trialDias || 7} dias`);
  } else if (plano.codigo === "PRO") {
    itens.push("Sem limites de utilização — sob consulta comercial com a Lucrare");
  } else {
    itens.push("Pagamento via PIX (Banco Inter)");
  }

  return itens;
}

export function precoRotulo(plano: Plano): string {
  if (plano.valorMensal == null) return "Sob consulta";
  if (plano.valorMensal === 0) return "Grátis";
  return `${formatarMoeda(plano.valorMensal)} / mês`;
}

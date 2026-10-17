import type { Plano } from "@/lib/api/types";
import { formatarMoeda } from "@/lib/format";

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
    "Gestão de inquilinos e contratos",
    "Dashboard e indicadores operacionais",
  ];

  if (plano.codigo === "TRIAL") {
    itens.push(`Período de avaliação de ${plano.trialDias || 7} dias`);
  } else if (plano.codigo === "PRO") {
    itens.push("Condições comerciais negociadas com a Lucrare");
  } else {
    itens.push("Pagamento via PIX ou cartão (Mercado Pago — em breve)");
  }

  return itens;
}

export function precoRotulo(plano: Plano): string {
  if (plano.valorMensal == null) return "Sob consulta";
  if (plano.valorMensal === 0) return "Grátis";
  return `${formatarMoeda(plano.valorMensal)} / mês`;
}

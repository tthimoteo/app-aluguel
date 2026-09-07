export type SemanticaStatus = "sucesso" | "perigo" | "aviso" | "info" | "neutro" | "destaque";

export function semanticaStatus(status: string): SemanticaStatus {
  const s = status.replace(/[\s_-]/g, "").toLowerCase();
  if (["ativo", "ativa", "emitida", "paga", "dentrodoprazo"].includes(s)) return "sucesso";
  if (["inativo", "cancelado", "cancelada", "rejeitada", "bloqueado"].includes(s)) return "perigo";
  if (["encerrado", "suspensa", "pendente", "pendentepagamento"].includes(s)) return "aviso";
  if (["administrador", "admin"].includes(s)) return "destaque";
  if (["gestor", "analista", "emprocessamento", "trial"].includes(s)) return "info";
  return "neutro";
}

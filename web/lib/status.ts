export type SemanticaStatus = "sucesso" | "perigo" | "aviso" | "info" | "neutro" | "destaque";

export function semanticaStatus(status: string): SemanticaStatus {
  const s = status.toLowerCase();
  if (["ativo", "emitida", "paga", "dentro do prazo"].includes(s)) return "sucesso";
  if (["inativo", "cancelado", "cancelada", "rejeitada", "bloqueado"].includes(s)) return "perigo";
  if (["encerrado", "suspensa", "pendente"].includes(s)) return "aviso";
  if (["administrador", "admin"].includes(s)) return "destaque";
  if (["gestor", "analista", "emprocessamento"].includes(s)) return "info";
  return "neutro";
}

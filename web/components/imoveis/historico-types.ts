import type { Contrato, Inquilino } from "@/lib/api/types";

export type HistoricoInquilinoItem = {
  inquilinoId: string;
  nome: string;
  documento: string | null;
  status: string | null;
  removido: boolean;
  dataInicio: string | null;
  dataFim: string | null;
  /** Cadastro completo quando ainda existir (não soft-deleted). */
  detalhe: Inquilino | null;
};

export type HistoricoContratoItem = Contrato;

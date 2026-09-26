import type { Contrato } from "@/lib/api/types";

export type HistoricoInquilinoItem = {
  inquilinoId: string;
  nome: string;
  documento: string | null;
  status: string | null;
  removido: boolean;
  dataInicio: string | null;
  dataFim: string | null;
};

export type HistoricoContratoItem = Contrato;

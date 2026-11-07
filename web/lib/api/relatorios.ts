/** Tipos do painel de relatórios (espelha RelatorioPainelDto do backend). */

export type RelatorioFiltro = {
  ano: number | null;
  mes: number | null;
  dataInicio: string | null;
  dataFim: string | null;
  clienteId: string | null;
  imovelId: string | null;
  diasAtrasoMinimos: number | null;
};

export type SerieMensal = {
  competencia: string;
  rotulo: string;
  aluguel: number;
  iptu: number;
  condominio: number;
  total: number;
};

export type SerieGeradoRecebido = {
  competencia: string;
  rotulo: string;
  gerado: number;
  recebido: number;
};

export type LinhaInadimplente = {
  nfseId: string;
  inquilinoNome: string;
  imovelNome: string;
  imovelId: string;
  valor: number;
  diasAtraso: number;
  competencia: string;
  dataVencimento: string;
};

export type RankingAtraso = {
  imovelId: string;
  imovelNome: string;
  mediaDiasAtraso: number;
  quantidadeAtrasos: number;
};

export type FluxoCaixaMes = {
  competencia: string;
  rotulo: string;
  previsao: number;
};

export type RentabilidadeImovel = {
  imovelId: string;
  imovelNome: string;
  receitaPeriodo: number;
  receitaMediaMensal: number;
  competenciasComReceita: number;
};

export type VacanciaImovel = {
  imovelId: string;
  imovelNome: string;
  diasVago: number;
  desde: string | null;
};

export type ContratoVencendo = {
  contratoId: string;
  inquilinoNome: string;
  imovelNome: string;
  dataFimPrevista: string;
  diasRestantes: number;
};

export type CompetenciaValor = {
  competencia: string;
  rotulo: string;
  valor: number;
  quantidade: number;
};

export type ProprietarioReceita = {
  clienteId: string;
  clienteNome: string;
  receita: number;
  percentualDaCarteira: number;
};

export type AgingFaixa = {
  faixa: string;
  quantidade: number;
  valor: number;
};

export type ScoreInquilino = {
  inquilinoId: string;
  inquilinoNome: string;
  quantidadeAtrasos: number;
  mediaDiasAtraso: number;
  risco: string;
};

export type RelatorioPainel = {
  filtroAplicado: RelatorioFiltro;
  dashboard: {
    receitaTotalPeriodo: number;
    receitaLiquidaAluguel: number;
    valorIptuRepassado: number;
    valorCondominioRepassado: number;
    totalRecebido: number;
    totalEmAberto: number;
    inadimplenciaPercentual: number;
    contratosAtivos: number;
    imoveisOcupados: number;
    imoveisVagos: number;
    evolucaoMensal: SerieMensal[];
    agingRecebiveis: AgingFaixa[];
    concentracaoTop5Percentual: number;
  };
  rendimento: {
    aluguel: number;
    iptu: number;
    condominio: number;
    multas: number;
    juros: number;
    receitaTotal: number;
    evolucao: SerieMensal[];
  };
  cobrancas: {
    geradas: number;
    pagas: number;
    emAberto: number;
    taxaRecebimentoPercentual: number;
    porCompetencia: SerieGeradoRecebido[];
  };
  inadimplentes: {
    totalInadimplente: number;
    percentualCarteira: number;
    quantidadeDevedores: number;
    itens: LinhaInadimplente[];
  };
  rankingAtrasos: RankingAtraso[];
  fluxoCaixaPrevisto: FluxoCaixaMes[];
  rentabilidadeImoveis: RentabilidadeImovel[];
  vacancia: {
    imoveisTotais: number;
    ocupados: number;
    vagos: number;
    taxaVacanciaPercentual: number;
    imoveisVagosDetalhe: VacanciaImovel[];
  };
  contratosVencendo: ContratoVencendo[];
  fiscal: {
    emitidas: number;
    canceladas: number;
    valorFaturadoEmitidas: number;
    porCompetencia: CompetenciaValor[];
    notasSemPagamentoCorrespondente: number;
    cobrancasSemNotaEmitida: number;
  };
  proprietarios: ProprietarioReceita[];
  dre: {
    receitaAluguel: number;
    multas: number;
    juros: number;
    iptuRepassado: number;
    condominioRepassado: number;
    totalMovimentado: number;
  };
  scoresInquilinos: ScoreInquilino[];
};

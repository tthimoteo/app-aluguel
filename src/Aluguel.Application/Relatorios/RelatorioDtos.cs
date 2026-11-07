namespace Aluguel.Application.Relatorios;

public sealed record RelatorioFiltroDto(
    int? Ano,
    int? Mes,
    DateOnly? DataInicio,
    DateOnly? DataFim,
    Guid? ClienteId,
    Guid? ImovelId,
    int? DiasAtrasoMinimos);

public sealed record IndicadorDecimalDto(string Chave, string Rotulo, decimal Valor, string? Formato = null);
public sealed record IndicadorInteiroDto(string Chave, string Rotulo, int Valor);
public sealed record SerieMensalDto(string Competencia, string Rotulo, decimal Aluguel, decimal Iptu, decimal Condominio, decimal Total);
public sealed record SerieGeradoRecebidoDto(string Competencia, string Rotulo, decimal Gerado, decimal Recebido);
public sealed record LinhaInadimplenteDto(
    Guid NfseId, string InquilinoNome, string ImovelNome, Guid ImovelId,
    decimal Valor, int DiasAtraso, string Competencia, DateOnly DataVencimento);
public sealed record RankingAtrasoDto(Guid ImovelId, string ImovelNome, decimal MediaDiasAtraso, int QuantidadeAtrasos);
public sealed record FluxoCaixaMesDto(string Competencia, string Rotulo, decimal Previsao);
public sealed record RentabilidadeImovelDto(
    Guid ImovelId, string ImovelNome, decimal ReceitaPeriodo, decimal ReceitaMediaMensal, int CompetenciasComReceita);
public sealed record VacanciaImovelDto(Guid ImovelId, string ImovelNome, int DiasVago, DateOnly? Desde);
public sealed record ContratoVencendoDto(
    Guid ContratoId, string InquilinoNome, string ImovelNome, DateOnly DataFimPrevista, int DiasRestantes);
public sealed record CompetenciaValorDto(string Competencia, string Rotulo, decimal Valor, int Quantidade);
public sealed record ProprietarioReceitaDto(Guid ClienteId, string ClienteNome, decimal Receita, decimal PercentualDaCarteira);
public sealed record AgingFaixaDto(string Faixa, int Quantidade, decimal Valor);
public sealed record ScoreInquilinoDto(
    Guid InquilinoId, string InquilinoNome, int QuantidadeAtrasos, decimal MediaDiasAtraso, string Risco);

public sealed record DashboardRelatorioDto(
    decimal ReceitaTotalPeriodo,
    decimal ReceitaLiquidaAluguel,
    decimal ValorIptuRepassado,
    decimal ValorCondominioRepassado,
    decimal TotalRecebido,
    decimal TotalEmAberto,
    decimal InadimplenciaPercentual,
    int ContratosAtivos,
    int ImoveisOcupados,
    int ImoveisVagos,
    IReadOnlyList<SerieMensalDto> EvolucaoMensal,
    IReadOnlyList<AgingFaixaDto> AgingRecebiveis,
    decimal ConcentracaoTop5Percentual);

public sealed record RendimentoPeriodoDto(
    decimal Aluguel, decimal Iptu, decimal Condominio, decimal Multas, decimal Juros, decimal ReceitaTotal,
    IReadOnlyList<SerieMensalDto> Evolucao);

public sealed record CobrancasGeradasRecebidasDto(
    decimal Geradas, decimal Pagas, decimal EmAberto, decimal TaxaRecebimentoPercentual,
    IReadOnlyList<SerieGeradoRecebidoDto> PorCompetencia);

public sealed record InadimplentesDto(
    decimal TotalInadimplente, decimal PercentualCarteira, int QuantidadeDevedores,
    IReadOnlyList<LinhaInadimplenteDto> Itens);

public sealed record VacanciaDto(
    int ImoveisTotais, int Ocupados, int Vagos, decimal TaxaVacanciaPercentual,
    IReadOnlyList<VacanciaImovelDto> ImoveisVagosDetalhe);

public sealed record FiscalNfseDto(
    int Emitidas, int Canceladas, decimal ValorFaturadoEmitidas,
    IReadOnlyList<CompetenciaValorDto> PorCompetencia,
    int NotasSemPagamentoCorrespondente,
    int CobrancasSemNotaEmitida);

public sealed record DreSimplificadoDto(
    decimal ReceitaAluguel, decimal Multas, decimal Juros,
    decimal IptuRepassado, decimal CondominioRepassado, decimal TotalMovimentado);

public sealed record RelatorioPainelDto(
    RelatorioFiltroDto FiltroAplicado,
    DashboardRelatorioDto Dashboard,
    RendimentoPeriodoDto Rendimento,
    CobrancasGeradasRecebidasDto Cobrancas,
    InadimplentesDto Inadimplentes,
    IReadOnlyList<RankingAtrasoDto> RankingAtrasos,
    IReadOnlyList<FluxoCaixaMesDto> FluxoCaixaPrevisto,
    IReadOnlyList<RentabilidadeImovelDto> RentabilidadeImoveis,
    VacanciaDto Vacancia,
    IReadOnlyList<ContratoVencendoDto> ContratosVencendo,
    FiscalNfseDto Fiscal,
    IReadOnlyList<ProprietarioReceitaDto> Proprietarios,
    DreSimplificadoDto Dre,
    IReadOnlyList<ScoreInquilinoDto> ScoresInquilinos);

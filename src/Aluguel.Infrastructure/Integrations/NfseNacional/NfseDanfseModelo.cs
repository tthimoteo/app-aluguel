namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>Dados do DANFSe extraídos exclusivamente do XML autorizado (NT 008).</summary>
internal sealed class NfseDanfseModelo
{
    public required string ChaveAcesso { get; init; }
    public required string NumeroNfse { get; init; }
    public required string Competencia { get; init; }
    public required string CompetenciaIso { get; init; }
    public required string DhProcessamento { get; init; }
    public required string NumeroDps { get; init; }
    public required string SerieDps { get; init; }
    public required string DhEmissaoDps { get; init; }
    public required string EmitenteDescricao { get; init; }
    public required string SituacaoDescricao { get; init; }
    public required string FinalidadeDescricao { get; init; }
    public required string LocalEmissao { get; init; }
    public required string AmbienteGerador { get; init; }
    public required string TipoAmbiente { get; init; }
    public required bool Homologacao { get; init; }
    public required string ConsultaPublicaUrl { get; init; }

    public required ParteDanfse Prestador { get; init; }
    public required ParteDanfse? Tomador { get; init; }
    public required string TributacaoNacionalCodigo { get; init; }
    public required string TributacaoNacionalDescricao { get; init; }
    public required string CodigoNbs { get; init; }
    public required string DescricaoNbs { get; init; }
    public required string LocalPrestacao { get; init; }
    public required string DescricaoServico { get; init; }

    public required string TribIssqnCodigo { get; init; }
    public required string TribIssqnDescricao { get; init; }
    public required bool SemIssqn { get; init; }
    public required string RetencaoIssqn { get; init; }
    public required string PercentualTotTribSn { get; init; }

    public required string Cst { get; init; }
    public required string ClassificacaoTributaria { get; init; }
    public required string CodigoIndicadorOperacao { get; init; }
    public required string LocalidadeIncidenciaIbs { get; init; }
    public required string BaseCalculoIbsCbs { get; init; }
    public required string AliquotaIbsUf { get; init; }
    public required string ReducaoAliqIbsUf { get; init; }
    public required string AliquotaEfetivaIbsUf { get; init; }
    public required string ValorIbsUf { get; init; }
    public required string AliquotaIbsMun { get; init; }
    public required string ReducaoAliqIbsMun { get; init; }
    public required string AliquotaEfetivaIbsMun { get; init; }
    public required string ValorIbsMun { get; init; }
    public required string ValorTotalIbs { get; init; }
    public required string AliquotaCbs { get; init; }
    public required string ReducaoAliqCbs { get; init; }
    public required string AliquotaEfetivaCbs { get; init; }
    public required string ValorCbs { get; init; }

    public required string ValorServico { get; init; }
    public required string ValorLiquido { get; init; }
    public required string ValorTotalNf { get; init; }
    public required string TotalIbsCbs { get; init; }

    public required string? ImovelMunicipio { get; init; }
    public required string? ImovelEndereco { get; init; }
    public required string? ImovelPercentualCopropriedade { get; init; }
    public required string? ImovelValorOperacao { get; init; }
}

internal sealed class ParteDanfse
{
    public required string Documento { get; init; }
    public required string Nome { get; init; }
    public required string InscricaoMunicipal { get; init; }
    public required string Telefone { get; init; }
    public required string Email { get; init; }
    public required string Endereco { get; init; }
    public required string MunicipioUf { get; init; }
    public required string Cep { get; init; }
    public required string CodigoMunicipioIbge { get; init; }
    public required string SimplesNacional { get; init; }
    public required string RegimeApuracaoSn { get; init; }
}

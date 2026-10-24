namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>Configuração da integração SEFIN / API Nacional da NFS-e.</summary>
public sealed class NfseOptions
{
    public const string SectionName = "Nfse";

    /// <summary>Mock | ProducaoRestrita | Producao</summary>
    public string Ambiente { get; set; } = "Mock";

    /// <summary>Identificador da aplicação no XML (<c>verAplic</c>, máx. 20).</summary>
    public string VerAplic { get; set; } = "LucrareAluguel1.0";

    /// <summary>Série da DPS (até 5 dígitos).</summary>
    public string SeriePadrao { get; set; } = "1";

    /// <summary>IBGE do município emissor quando o cadastro não resolve (São Paulo = 3550308).</summary>
    public string CodigoMunicipioIbgePadrao { get; set; } = "3550308";

    /// <summary>
    /// Código de tributação nacional (6 dígitos LC 116) quando <c>CodigoServico</c> do cliente
    /// for municipal (≠ 6 dígitos). Ex.: engenharia 170101.
    /// </summary>
    public string CodigoTributacaoNacionalPadrao { get; set; } = "170101";

    /// <summary>Percentual aproximado de tributos do Simples (<c>pTotTribSN</c>).</summary>
    public decimal PercentualTotTribSn { get; set; } = 6.00m;

    /// <summary>Finalidade da NFS-e (<c>finNFSe</c>): 0 = regular (NT 009 / layout 20/10/2026).</summary>
    public string FinNfse { get; set; } = "0";

    /// <summary>Código indicador da operação IBS/CBS (<c>cIndOp</c>), quando informado.</summary>
    public string? CodigoIndicadorOperacaoIbsCbs { get; set; } = "010101";

    /// <summary>CST IBS/CBS (3 dígitos).</summary>
    public string CstIbsCbs { get; set; } = "000";

    /// <summary>Classificação tributária IBS/CBS (6 dígitos).</summary>
    public string ClassificacaoTributariaIbsCbs { get; set; } = "000001";

    /// <summary>
    /// Inclui grupo <c>IBSCBS</c>. Em homologação exige NBS/códigos da calculadora RTC;
    /// mantenha <c>false</c> até o emitente ter correlação NBS/cIndOp válida.
    /// </summary>
    public bool IncluirIbsCbs { get; set; }

    /// <summary>Código NBS (9 dígitos) — obrigatório quando há IBS/CBS (E0322).</summary>
    public string CodigoNbsPadrao { get; set; } = "115021000";

    /// <summary>
    /// Envia <c>prest/IM</c>. Em produção restrita, São Paulo costuma responder E0120
    /// se o CNC municipal não tiver dados complementares — deixe <c>false</c> até o CNC estar ok.
    /// </summary>
    public bool EnviarImPrestador { get; set; }

    public bool IsMock =>
        string.Equals(Ambiente, "Mock", StringComparison.OrdinalIgnoreCase);

    public bool IsProducao =>
        string.Equals(Ambiente, "Producao", StringComparison.OrdinalIgnoreCase);

    /// <summary>tpAmb: 1 = produção, 2 = produção restrita (homologação).</summary>
    public int TipoAmbiente => IsProducao ? 1 : 2;

    public string BaseUrl => IsProducao
        ? "https://sefin.nfse.gov.br/SefinNacional/"
        : "https://sefin.producaorestrita.nfse.gov.br/SefinNacional/";
}

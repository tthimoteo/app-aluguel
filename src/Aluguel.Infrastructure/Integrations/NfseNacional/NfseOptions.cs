using Aluguel.Domain.Fiscal;

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

    /// <summary>cTribNac padrão Lucrare quando o cliente não informar 6 dígitos.</summary>
    public string CodigoTributacaoNacionalPadrao { get; set; } = NfseCodigosPadrao.CodigoTributacaoNacional;

    /// <summary>Percentual aproximado de tributos do Simples (<c>pTotTribSN</c>).</summary>
    public decimal PercentualTotTribSn { get; set; } = 6.00m;

    /// <summary>Finalidade da NFS-e (<c>finNFSe</c>): 0 = regular (NT 009 / layout 20/10/2026).</summary>
    public string FinNfse { get; set; } = "0";

    /// <summary>cIndOp padrão Lucrare.</summary>
    public string? CodigoIndicadorOperacaoIbsCbs { get; set; } = NfseCodigosPadrao.CodigoIndicadorOperacao;

    /// <summary>CST IBS/CBS (3 dígitos).</summary>
    public string CstIbsCbs { get; set; } = NfseCodigosPadrao.CstIbsCbs;

    /// <summary>cClassTrib padrão Lucrare.</summary>
    public string ClassificacaoTributariaIbsCbs { get; set; } = NfseCodigosPadrao.ClassificacaoTributaria;

    /// <summary>Inclui grupo <c>IBSCBS</c> (cIndOp + cClassTrib Lucrare).</summary>
    public bool IncluirIbsCbs { get; set; } = true;

    /// <summary>
    /// cNBS padrão (9 dígitos) quando o imóvel não informar.
    /// Com IBS/CBS a SEFIN exige NBS (E0322); padrão Lucrare = locação residencial.
    /// </summary>
    public string CodigoNbsPadrao { get; set; } = NfseCodigosPadrao.CodigoNbsResidencial;

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

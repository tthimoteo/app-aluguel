namespace Aluguel.Domain.Fiscal;

/// <summary>
/// Códigos fiscais padrão da Lucrare para emissão na SEFIN Nacional (DPS / IBS-CBS).
/// Usados como default de configuração e referência de validação no cadastro do cliente.
/// </summary>
public static class NfseCodigosPadrao
{
    /// <summary>cTribNac — código de tributação nacional (6 dígitos).</summary>
    public const string CodigoTributacaoNacional = "990301";

    /// <summary>cIndOp — indicador da operação IBS/CBS (6 dígitos).</summary>
    public const string CodigoIndicadorOperacao = "020201";

    /// <summary>cClassTrib — classificação tributária IBS/CBS (6 dígitos).</summary>
    public const string ClassificacaoTributaria = "200027";

    /// <summary>
    /// CST IBS/CBS padrão (3 dígitos) — deve coincidir com os 3 primeiros de <see cref="ClassificacaoTributaria"/> (E0959).
    /// </summary>
    public const string CstIbsCbs = "200";

    /// <summary>
    /// cNBS — Locação de imóveis residenciais (1.1002.10.00).
    /// Obrigatório com IBS/CBS (E0322) para cTribNac 990301.
    /// </summary>
    public const string CodigoNbsResidencial = "110021000";

    /// <summary>cNBS — Locação de imóveis não residenciais (1.1002.20.00).</summary>
    public const string CodigoNbsNaoResidencial = "110022000";
}

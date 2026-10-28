namespace Aluguel.Infrastructure.Integrations.BoletoBancario;

/// <summary>
/// Configuração global da geração de boletos.
/// <c>Ambiente=Mock</c> (padrão): adapters geram boleto localmente com layout do banco.
/// <c>Homologacao</c>/<c>Producao</c>: cada adapter chama a API HTTP do provedor.
/// </summary>
public sealed class BoletoOptions
{
    public const string SectionName = "Boleto";

    /// <summary>Mock | Homologacao | Producao</summary>
    public string Ambiente { get; set; } = "Mock";

    public bool UsarMock =>
        string.Equals(Ambiente, "Mock", StringComparison.OrdinalIgnoreCase);

    /// <summary>URLs base por provedor (sobrescrevem o padrão do adapter).</summary>
    public Dictionary<string, string> BaseUrls { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

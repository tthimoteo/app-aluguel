namespace Aluguel.Infrastructure.Integrations.PixInter;

/// <summary>
/// Credenciais da plataforma para receber PIX de assinatura na conta
/// TTHIMOTEO Consultoria e Serviços de Eng. Ltda (Banco Inter).
/// </summary>
public sealed class InterPixOptions
{
    public const string SectionName = "InterPix";

    /// <summary>Mock | Homologacao | Producao.</summary>
    public string Ambiente { get; set; } = "Mock";

    public bool UsarMock =>
        string.Equals(Ambiente, "Mock", StringComparison.OrdinalIgnoreCase);

    public string UrlBase { get; set; } = "https://cdpj.partners.bancointer.com.br/";

    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";

    /// <summary>Conta corrente Inter do beneficiário (ex.: 44588184-4).</summary>
    public string ContaCorrente { get; set; } = "44588184-4";

    /// <summary>Chave PIX cadastrada na conta Inter (CNPJ, e-mail, EVP etc.).</summary>
    public string ChavePix { get; set; } = "";

    public string? CertificadoPath { get; set; }
    public string? CertificadoSenha { get; set; }

    public string BeneficiarioNome { get; set; } =
        "TTHIMOTEO Consultoria e Serviços de Eng. Ltda";

    public string BeneficiarioCnpj { get; set; } = "15809930000168";

    public string BeneficiarioBanco { get; set; } = "Banco Inter";

    public string BeneficiarioContaCorrente { get; set; } = "44588184-4";

    public int ExpiracaoSegundosPadrao { get; set; } = 3600;
}

namespace Aluguel.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Mock | Graph (OAuth2 Microsoft 365) | Smtp (legado básico).</summary>
    public string Ambiente { get; set; } = "Mock";

    // --- Microsoft Graph (OAuth2 client credentials) ---
    public string GraphTenantId { get; set; } = "";
    public string GraphClientId { get; set; } = "";
    public string GraphClientSecret { get; set; } = "";
    /// <summary>UPN da caixa que envia (ex.: thiago.thimoteo@lucrarecontabilidade.com.br).</summary>
    public string GraphSenderUser { get; set; } = "thiago.thimoteo@lucrarecontabilidade.com.br";

    // --- SMTP básico (legado; O365 exige senha de app / SMTP AUTH) ---
    public string SmtpHost { get; set; } = "smtp.office365.com";
    public int SmtpPort { get; set; } = 587;
    public bool UsarStartTls { get; set; } = true;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string Remetente { get; set; } = "";
    public string RemetenteNome { get; set; } = "APP Aluguel · Lucrare";

    /// <summary>Caixa que recebe leads do formulário «Solicitar Demonstração».</summary>
    public string DestinatarioDemonstracao { get; set; } = "tthimoteoconsultoria@gmail.com";
}

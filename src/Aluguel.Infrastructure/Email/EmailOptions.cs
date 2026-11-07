namespace Aluguel.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Mock (log) ou Smtp.</summary>
    public string Ambiente { get; set; } = "Mock";

    /// <summary>Office 365 / Exchange Online: smtp.office365.com</summary>
    public string SmtpHost { get; set; } = "smtp.office365.com";
    public int SmtpPort { get; set; } = 587;
    public bool UsarStartTls { get; set; } = true;
    /// <summary>Caixa @lucrarecontabilidade.com.br com SMTP AUTH habilitado.</summary>
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    /// <summary>Deve ser a mesma caixa (ou alias permitido) do SmtpUser no O365.</summary>
    public string Remetente { get; set; } = "";
    public string RemetenteNome { get; set; } = "APP Aluguel · Lucrare";

    /// <summary>Caixa que recebe leads do formulário «Solicitar Demonstração».</summary>
    public string DestinatarioDemonstracao { get; set; } = "tthimoteoconsultoria@gmail.com";
}

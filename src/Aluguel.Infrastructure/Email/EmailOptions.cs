namespace Aluguel.Infrastructure.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    /// <summary>Mock (log) ou Smtp.</summary>
    public string Ambiente { get; set; } = "Mock";

    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
    public bool UsarStartTls { get; set; } = true;
    public string SmtpUser { get; set; } = "";
    public string SmtpPassword { get; set; } = "";
    public string Remetente { get; set; } = "tthimoteoconsultoria@gmail.com";
    public string RemetenteNome { get; set; } = "APP Aluguel · Lucrare";

    /// <summary>Caixa que recebe leads do formulário «Solicitar Demonstração».</summary>
    public string DestinatarioDemonstracao { get; set; } = "tthimoteoconsultoria@gmail.com";
}

namespace Aluguel.Application.Abstractions;

/// <summary>Porta de envio de e-mail transacional (SMTP / provedor).</summary>
public interface IEmailSender
{
    Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct = default);
}

public sealed record MensagemEmail(
    string Destinatario,
    string Assunto,
    string CorpoTexto,
    string? CorpoHtml = null,
    string? RemetenteResposta = null);

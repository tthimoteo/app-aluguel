using Aluguel.Application.Abstractions;
using Microsoft.Extensions.Logging;

namespace Aluguel.Infrastructure.Email;

/// <summary>Ambiente Mock: registra o e-mail sem enviar (útil em Development sem SMTP).</summary>
public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct = default)
    {
        logger.LogInformation(
            "E-mail (mock) para {Destinatario} | Assunto: {Assunto}\n{Corpo}",
            mensagem.Destinatario, mensagem.Assunto, mensagem.CorpoTexto);
        return Task.CompletedTask;
    }
}

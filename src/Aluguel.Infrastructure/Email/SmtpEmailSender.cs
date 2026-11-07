using Aluguel.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Aluguel.Infrastructure.Email;

public sealed class SmtpEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct = default)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.SmtpHost))
            throw new InvalidOperationException("SMTP não configurado (Email:SmtpHost).");
        if (string.IsNullOrWhiteSpace(opts.SmtpUser) || string.IsNullOrWhiteSpace(opts.SmtpPassword))
            throw new InvalidOperationException(
                "Credenciais SMTP ausentes. Configure EMAIL_SMTP_USER e EMAIL_SMTP_PASSWORD " +
                "(caixa Office 365 @lucrarecontabilidade.com.br com SMTP AUTH).");

        var remetente = string.IsNullOrWhiteSpace(opts.Remetente) ? opts.SmtpUser : opts.Remetente;

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(opts.RemetenteNome, remetente));
        mime.To.Add(MailboxAddress.Parse(mensagem.Destinatario));
        mime.Subject = mensagem.Assunto;
        if (!string.IsNullOrWhiteSpace(mensagem.RemetenteResposta))
            mime.ReplyTo.Add(MailboxAddress.Parse(mensagem.RemetenteResposta));

        var builder = new BodyBuilder { TextBody = mensagem.CorpoTexto };
        if (!string.IsNullOrWhiteSpace(mensagem.CorpoHtml))
            builder.HtmlBody = mensagem.CorpoHtml;
        mime.Body = builder.ToMessageBody();

        using var client = new SmtpClient();
        var secure = opts.UsarStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        await client.ConnectAsync(opts.SmtpHost, opts.SmtpPort, secure, ct);
        await client.AuthenticateAsync(opts.SmtpUser, opts.SmtpPassword, ct);
        await client.SendAsync(mime, ct);
        await client.DisconnectAsync(true, ct);

        logger.LogInformation(
            "E-mail enviado para {Destinatario}: {Assunto}",
            mensagem.Destinatario, mensagem.Assunto);
    }
}

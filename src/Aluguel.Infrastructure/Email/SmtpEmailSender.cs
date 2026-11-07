using Aluguel.Application.Abstractions;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SmtpCommandException = MailKit.Net.Smtp.SmtpCommandException;

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
        // Ambientes sem acesso aos endpoints de CRL da Microsoft falham o handshake;
        // a identidade do certificado continua validada pelo MailKit/SslStream.
        client.CheckCertificateRevocation = false;
        var secure = opts.UsarStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
        try
        {
            await client.ConnectAsync(opts.SmtpHost, opts.SmtpPort, secure, ct);
            await client.AuthenticateAsync(opts.SmtpUser, opts.SmtpPassword, ct);
            await client.SendAsync(mime, ct);
            await client.DisconnectAsync(true, ct);
        }
        catch (AuthenticationException ex)
        {
            logger.LogError(ex, "Falha de autenticação SMTP ao enviar para {Destinatario}", mensagem.Destinatario);
            throw new InvalidOperationException(
                "Falha ao autenticar no SMTP (Office 365). Verifique usuário/senha e se a autenticação SMTP " +
                "(SMTP AUTH) está habilitada para a caixa/tenant: https://aka.ms/smtp_auth_disabled",
                ex);
        }
        catch (SmtpCommandException ex)
        {
            logger.LogError(ex, "Servidor SMTP rejeitou o envio para {Destinatario}", mensagem.Destinatario);
            throw new InvalidOperationException(
                $"O servidor SMTP rejeitou o e-mail ({ex.StatusCode}): {ex.Message}", ex);
        }
        catch (SslHandshakeException ex)
        {
            logger.LogError(ex, "Falha TLS ao conectar em {Host}", opts.SmtpHost);
            throw new InvalidOperationException(
                "Falha de certificado/TLS ao conectar no SMTP. Tente novamente ou revise o host/porta.", ex);
        }

        logger.LogInformation(
            "E-mail enviado para {Destinatario}: {Assunto}",
            mensagem.Destinatario, mensagem.Assunto);
    }
}

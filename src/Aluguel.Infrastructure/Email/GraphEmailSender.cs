using Aluguel.Application.Abstractions;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using AuthenticationFailedException = Azure.Identity.AuthenticationFailedException;

namespace Aluguel.Infrastructure.Email;

/// <summary>
/// Envio via Microsoft Graph com OAuth2 (client credentials) — sem senha de app SMTP.
/// Permissão de aplicativo necessária: Mail.Send (consentimento admin).
/// </summary>
public sealed class GraphEmailSender : IEmailSender
{
    private readonly EmailOptions _opts;
    private readonly ILogger<GraphEmailSender> _logger;
    private readonly GraphServiceClient _graph;

    public GraphEmailSender(IOptions<EmailOptions> options, ILogger<GraphEmailSender> logger)
    {
        _opts = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_opts.GraphTenantId)
            || string.IsNullOrWhiteSpace(_opts.GraphClientId)
            || string.IsNullOrWhiteSpace(_opts.GraphClientSecret))
        {
            throw new InvalidOperationException(
                "Microsoft Graph não configurado. Defina EMAIL_GRAPH_TENANT_ID, EMAIL_GRAPH_CLIENT_ID e EMAIL_GRAPH_CLIENT_SECRET.");
        }

        var credential = new ClientSecretCredential(
            _opts.GraphTenantId.Trim(),
            _opts.GraphClientId.Trim(),
            _opts.GraphClientSecret.Trim());

        _graph = new GraphServiceClient(credential, ["https://graph.microsoft.com/.default"]);
    }

    public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken ct = default)
    {
        var remetente = string.IsNullOrWhiteSpace(_opts.GraphSenderUser)
            ? _opts.Remetente
            : _opts.GraphSenderUser;
        if (string.IsNullOrWhiteSpace(remetente))
            throw new InvalidOperationException(
                "Remetente Graph ausente. Configure EMAIL_GRAPH_SENDER (UPN da caixa Lucrare).");

        var bodyContent = !string.IsNullOrWhiteSpace(mensagem.CorpoHtml)
            ? mensagem.CorpoHtml
            : mensagem.CorpoTexto;
        var bodyType = !string.IsNullOrWhiteSpace(mensagem.CorpoHtml) ? BodyType.Html : BodyType.Text;

        var mail = new Message
        {
            Subject = mensagem.Assunto,
            Body = new ItemBody
            {
                ContentType = bodyType,
                Content = bodyContent,
            },
            ToRecipients =
            [
                new Recipient
                {
                    EmailAddress = new EmailAddress { Address = mensagem.Destinatario },
                },
            ],
        };

        if (!string.IsNullOrWhiteSpace(mensagem.RemetenteResposta))
        {
            mail.ReplyTo =
            [
                new Recipient
                {
                    EmailAddress = new EmailAddress { Address = mensagem.RemetenteResposta },
                },
            ];
        }

        var body = new SendMailPostRequestBody
        {
            Message = mail,
            SaveToSentItems = true,
        };

        try
        {
            await _graph.Users[remetente].SendMail.PostAsync(body, cancellationToken: ct);
        }
        catch (ServiceException ex)
        {
            _logger.LogError(ex, "Microsoft Graph rejeitou envio para {Destinatario}", mensagem.Destinatario);
            throw new InvalidOperationException(
                $"Falha ao enviar e-mail via Microsoft Graph ({ex.ResponseStatusCode}): {ex.Message}. " +
                "Confirme permissão de aplicativo Mail.Send com consentimento admin e o UPN do remetente.",
                ex);
        }
        catch (AuthenticationFailedException ex)
        {
            _logger.LogError(ex, "OAuth2 Graph falhou (tenant/client/secret)");
            throw new InvalidOperationException(
                "Falha de autenticação OAuth2 no Microsoft Graph. Verifique Tenant ID, Client ID e Client Secret.",
                ex);
        }

        _logger.LogInformation(
            "E-mail Graph enviado de {Remetente} para {Destinatario}: {Assunto}",
            remetente, mensagem.Destinatario, mensagem.Assunto);
    }
}

using System.Net;
using System.Text;
using Aluguel.Application.Abstractions;
using MediatR;

namespace Aluguel.Application.CadastroPublico.SolicitarDemonstracao;

/// <summary>Lead comercial do plano Pro — envia os dados por e-mail à equipe comercial.</summary>
public sealed record SolicitarDemonstracaoCommand(
    string Nome,
    string Email,
    string Telefone,
    string? Mensagem) : IRequest<SolicitarDemonstracaoResultadoDto>;

public sealed record SolicitarDemonstracaoResultadoDto(bool Ok, string Mensagem);

public sealed class SolicitarDemonstracaoCommandHandler(IEmailSender email)
    : IRequestHandler<SolicitarDemonstracaoCommand, SolicitarDemonstracaoResultadoDto>
{
    public const string DestinatarioPadrao = "tthimoteoconsultoria@gmail.com";

    public async Task<SolicitarDemonstracaoResultadoDto> Handle(
        SolicitarDemonstracaoCommand request, CancellationToken cancellationToken)
    {
        var nome = request.Nome?.Trim() ?? "";
        var emailContato = request.Email?.Trim() ?? "";
        var telefone = request.Telefone?.Trim() ?? "";
        var mensagem = string.IsNullOrWhiteSpace(request.Mensagem) ? null : request.Mensagem.Trim();

        if (nome.Length < 3)
            throw new InvalidOperationException("Informe o nome completo.");
        if (string.IsNullOrWhiteSpace(emailContato) || !emailContato.Contains('@'))
            throw new InvalidOperationException("Informe um e-mail válido.");
        if (telefone.Length < 8)
            throw new InvalidOperationException("Informe um telefone de contato.");

        var texto = MontarCorpoTexto(nome, emailContato, telefone, mensagem);
        var html = MontarCorpoHtml(nome, emailContato, telefone, mensagem);

        await email.EnviarAsync(
            new MensagemEmail(
                Destinatario: DestinatarioPadrao,
                Assunto: $"[APP Aluguel] Solicitação de demonstração — {nome}",
                CorpoTexto: texto,
                CorpoHtml: html,
                RemetenteResposta: emailContato),
            cancellationToken);

        return new SolicitarDemonstracaoResultadoDto(
            true,
            "Recebemos seu pedido. A equipe entrará em contato em breve pelo e-mail ou telefone informado.");
    }

    private static string MontarCorpoTexto(string nome, string email, string telefone, string? mensagem)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Nova solicitação de demonstração do plano Pro (APP Aluguel).");
        sb.AppendLine();
        sb.AppendLine($"Nome: {nome}");
        sb.AppendLine($"E-mail: {email}");
        sb.AppendLine($"Telefone: {telefone}");
        sb.AppendLine($"Mensagem: {(mensagem ?? "(não informada)")}");
        sb.AppendLine();
        sb.AppendLine($"Recebido em: {DateTimeOffset.Now:dd/MM/yyyy HH:mm:ss zzz}");
        return sb.ToString();
    }

    private static string MontarCorpoHtml(string nome, string email, string telefone, string? mensagem)
    {
        static string E(string v) => WebUtility.HtmlEncode(v);
        return $"""
            <div style="font-family:Montserrat,Arial,sans-serif;color:#2c3e50;line-height:1.5">
              <h2 style="color:#003366;margin:0 0 12px">Solicitação de demonstração · Plano Pro</h2>
              <p style="margin:0 0 16px">Novo lead pelo formulário público do APP Aluguel.</p>
              <table style="border-collapse:collapse;width:100%;max-width:560px">
                <tr><td style="padding:6px 0;font-weight:700;width:120px">Nome</td><td style="padding:6px 0">{E(nome)}</td></tr>
                <tr><td style="padding:6px 0;font-weight:700">E-mail</td><td style="padding:6px 0"><a href="mailto:{E(email)}">{E(email)}</a></td></tr>
                <tr><td style="padding:6px 0;font-weight:700">Telefone</td><td style="padding:6px 0">{E(telefone)}</td></tr>
                <tr><td style="padding:6px 0;font-weight:700;vertical-align:top">Mensagem</td><td style="padding:6px 0">{E(mensagem ?? "(não informada)")}</td></tr>
              </table>
              <p style="margin:16px 0 0;color:#6c757d;font-size:12px">Responda diretamente a este e-mail para falar com o interessado (Reply-To).</p>
            </div>
            """;
    }
}

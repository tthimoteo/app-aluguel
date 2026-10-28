using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Boletos;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aluguel.Infrastructure.Integrations.BoletoBancario;

/// <summary>
/// Base dos adapters bancários: Mock gera boleto local; Homologação/Produção chama a API HTTP.
/// Subclasses definem código, URL padrão e o corpo da requisição específica do banco.
/// </summary>
public abstract class BoletoProviderBase(
    IHttpClientFactory httpClientFactory,
    IOptions<BoletoOptions> options,
    ILogger logger) : IBoletoProvider
{
    protected BoletoOptions Options => options.Value;

    public abstract string CodigoProvedor { get; }

    protected abstract string NomeProvedor { get; }

    protected abstract string CodigoBanco { get; }

    /// <summary>URL relativa ou absoluta do endpoint de criação de boleto (quando não-Mock).</summary>
    protected abstract string EndpointCriacaoRelativo { get; }

    protected virtual string? UrlBasePadrao => null;

    public async Task<BoletoGeracaoResultado> GerarAsync(
        BoletoGeracaoPedido pedido, CancellationToken ct = default)
    {
        if (Options.UsarMock)
            return GerarMock(pedido);

        return await GerarViaApiAsync(pedido, ct);
    }

    protected BoletoGeracaoResultado GerarMock(BoletoGeracaoPedido pedido)
    {
        var seq = Math.Abs(BitConverter.ToInt64(pedido.ReferenciaId.ToByteArray(), 0));
        var nosso = BoletoPdfSimples.NossoNumeroMock(CodigoBanco, seq);
        var linha = BoletoPdfSimples.LinhaDigitavelMock(CodigoBanco, seq, pedido.Valor);
        var pdf = BoletoPdfSimples.Gerar(NomeProvedor, CodigoBanco, pedido, nosso, linha);
        return new BoletoGeracaoResultado(
            Sucesso: true,
            NossoNumero: nosso,
            LinhaDigitavel: linha,
            CodigoBarras: linha.Replace(" ", "").Replace(".", ""),
            PdfBytes: pdf,
            IdExterno: $"mock-{CodigoProvedor}-{pedido.ReferenciaId:N}");
    }

    protected virtual async Task<BoletoGeracaoResultado> GerarViaApiAsync(
        BoletoGeracaoPedido pedido, CancellationToken ct)
    {
        var baseUrl = ResolverBaseUrl();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new BoletoGeracaoResultado(
                false,
                MensagemErro:
                $"URL base da API do provedor '{CodigoProvedor}' não configurada " +
                $"(Boleto:BaseUrls:{CodigoProvedor}). Use Ambiente=Mock em desenvolvimento.");
        }

        try
        {
            var client = httpClientFactory.CreateClient($"boleto-{CodigoProvedor}");
            client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
            await ConfigurarAutenticacaoAsync(client, pedido.Credenciais, ct);

            var body = MontarCorpoCriacao(pedido);
            using var response = await client.PostAsJsonAsync(EndpointCriacaoRelativo.TrimStart('/'), body, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Falha HTTP {Status} na API de boleto {Provedor}: {Body}",
                    (int)response.StatusCode, CodigoProvedor, Truncar(raw));
                return new BoletoGeracaoResultado(
                    false,
                    MensagemErro: $"API {NomeProvedor} retornou {(int)response.StatusCode}: {Truncar(raw)}");
            }

            return InterpretarResposta(raw, pedido);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Erro ao gerar boleto via {Provedor}", CodigoProvedor);
            return new BoletoGeracaoResultado(false, MensagemErro: $"Erro na API {NomeProvedor}: {ex.Message}");
        }
    }

    protected virtual Task ConfigurarAutenticacaoAsync(
        HttpClient client, IReadOnlyDictionary<string, string> credenciais, CancellationToken ct)
    {
        if (credenciais.TryGetValue("access_token", out var token) && !string.IsNullOrWhiteSpace(token))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return Task.CompletedTask;
        }

        if (credenciais.TryGetValue("client_id", out var id)
            && credenciais.TryGetValue("client_secret", out var secret)
            && !string.IsNullOrWhiteSpace(id)
            && !string.IsNullOrWhiteSpace(secret))
        {
            var basic = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{id}:{secret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
        }

        return Task.CompletedTask;
    }

    /// <summary>Corpo JSON enviado ao endpoint de criação — específico de cada banco.</summary>
    protected abstract object MontarCorpoCriacao(BoletoGeracaoPedido pedido);

    /// <summary>Extrai nosso número / linha / PDF da resposta JSON do banco.</summary>
    protected virtual BoletoGeracaoResultado InterpretarResposta(string rawJson, BoletoGeracaoPedido pedido)
    {
        using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(rawJson) ? "{}" : rawJson);
        var root = doc.RootElement;
        var nosso = LerString(root, "nossoNumero", "nosso_numero", "ourNumber", "id");
        var linha = LerString(root, "linhaDigitavel", "linha_digitavel", "digitableLine", "barcode");
        var idExt = LerString(root, "id", "txid", "boletoId", "idExterno");
        var pdfB64 = LerString(root, "pdf", "pdfBase64", "pdf_base64");

        byte[]? pdf = null;
        if (!string.IsNullOrWhiteSpace(pdfB64))
        {
            try { pdf = Convert.FromBase64String(pdfB64); }
            catch (FormatException) { /* ignora */ }
        }

        if (pdf is null || pdf.Length == 0)
        {
            var seq = Math.Abs(BitConverter.ToInt64(pedido.ReferenciaId.ToByteArray(), 0));
            nosso ??= BoletoPdfSimples.NossoNumeroMock(CodigoBanco, seq);
            linha ??= BoletoPdfSimples.LinhaDigitavelMock(CodigoBanco, seq, pedido.Valor);
            pdf = BoletoPdfSimples.Gerar(NomeProvedor, CodigoBanco, pedido, nosso, linha);
        }

        return new BoletoGeracaoResultado(
            Sucesso: true,
            NossoNumero: nosso,
            LinhaDigitavel: linha,
            PdfBytes: pdf,
            IdExterno: idExt);
    }

    protected string? ResolverBaseUrl()
    {
        if (Options.BaseUrls.TryGetValue(CodigoProvedor, out var cfg) && !string.IsNullOrWhiteSpace(cfg))
            return cfg;
        return UrlBasePadrao;
    }

    protected static string? Cred(IReadOnlyDictionary<string, string> c, string chave) =>
        c.TryGetValue(chave, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

    protected static string Truncar(string s, int max = 400) =>
        s.Length <= max ? s : s[..max] + "…";

    private static string? LerString(JsonElement root, params string[] nomes)
    {
        foreach (var n in nomes)
        {
            if (root.TryGetProperty(n, out var p) && p.ValueKind == JsonValueKind.String)
                return p.GetString();
            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in root.EnumerateObject())
                {
                    if (string.Equals(prop.Name, n, StringComparison.OrdinalIgnoreCase)
                        && prop.Value.ValueKind == JsonValueKind.String)
                        return prop.Value.GetString();
                }
            }
        }
        return null;
    }

    protected static ProvedorBoletoDef DefOrThrow(string codigo) =>
        ProvedorBoletoCatalogo.Obter(codigo)
        ?? throw new InvalidOperationException($"Provedor '{codigo}' ausente no catálogo.");
}

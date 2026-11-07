using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Aluguel.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aluguel.Infrastructure.Integrations.PixInter;

/// <summary>
/// Cliente da API PIX do Banco Inter (OAuth client_credentials + mTLS + PUT /pix/v2/cob/{txid}).
/// </summary>
public sealed class InterPixProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<InterPixOptions> options,
    ILogger<InterPixProvider> logger) : IInterPixProvider
{
    public InterPixBeneficiario ObterBeneficiario()
    {
        var o = options.Value;
        return new InterPixBeneficiario(
            o.BeneficiarioNome,
            o.BeneficiarioCnpj,
            o.BeneficiarioBanco,
            o.BeneficiarioContaCorrente);
    }

    public async Task<InterPixCobrancaResultado> CriarCobrancaImediataAsync(
        InterPixCobrancaPedido pedido, CancellationToken ct = default)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ChavePix))
        {
            return new InterPixCobrancaResultado(
                false,
                MensagemErro: "InterPix:ChavePix não configurada. Use Ambiente=Mock em desenvolvimento.");
        }

        if (string.IsNullOrWhiteSpace(opts.ClientId) || string.IsNullOrWhiteSpace(opts.ClientSecret))
        {
            return new InterPixCobrancaResultado(
                false,
                MensagemErro: "InterPix:ClientId/ClientSecret não configurados.");
        }

        try
        {
            var client = CriarClienteMtls(opts);
            var token = await ObterTokenAsync(client, opts, ct);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var body = new
            {
                calendario = new { expiracao = pedido.ExpiracaoSegundos },
                valor = new
                {
                    original = pedido.Valor.ToString("0.00", CultureInfo.InvariantCulture),
                },
                chave = opts.ChavePix,
                solicitacaoPagador = Truncar(pedido.SolicitacaoPagador, 140),
            };

            var url = $"pix/v2/cob/{Uri.EscapeDataString(pedido.TxId)}";
            using var response = await client.PutAsJsonAsync(url, body, ct);
            var raw = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Falha HTTP {Status} ao criar cobrança PIX Inter: {Body}",
                    (int)response.StatusCode, Truncar(raw, 500));
                return new InterPixCobrancaResultado(
                    false,
                    MensagemErro: $"API Banco Inter retornou {(int)response.StatusCode}: {Truncar(raw, 300)}");
            }

            using var doc = JsonDocument.Parse(raw);
            var root = doc.RootElement;
            var copia = root.TryGetProperty("pixCopiaECola", out var c) ? c.GetString()
                : root.TryGetProperty("pixCopiaCola", out var c2) ? c2.GetString() : null;
            var location = root.TryGetProperty("location", out var loc) ? loc.GetString() : null;
            var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;
            var txid = root.TryGetProperty("txid", out var t) ? t.GetString() : pedido.TxId;

            if (string.IsNullOrWhiteSpace(copia))
            {
                return new InterPixCobrancaResultado(
                    false,
                    MensagemErro: "Resposta do Banco Inter sem pixCopiaECola.");
            }

            return new InterPixCobrancaResultado(
                Sucesso: true,
                TxId: txid,
                PixCopiaCola: copia,
                Location: location,
                Status: status);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Erro ao criar cobrança PIX no Banco Inter");
            return new InterPixCobrancaResultado(false, MensagemErro: $"Erro na API Banco Inter: {ex.Message}");
        }
    }

    private HttpClient CriarClienteMtls(InterPixOptions opts)
    {
        var client = httpClientFactory.CreateClient("inter-pix");
        client.BaseAddress = new Uri(opts.UrlBase.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(60);

        // Certificado mTLS é aplicado via handler nomeado registrado no DI quando o path existe.
        return client;
    }

    private static async Task<string> ObterTokenAsync(
        HttpClient client, InterPixOptions opts, CancellationToken ct)
    {
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = opts.ClientId,
            ["client_secret"] = opts.ClientSecret,
            ["grant_type"] = "client_credentials",
            ["scope"] = "cob.write cob.read pix.read webhook.write webhook.read",
        });

        using var response = await client.PostAsync("oauth/v2/token", content, ct);
        var raw = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"OAuth Inter falhou ({(int)response.StatusCode}): {Truncar(raw, 200)}");

        using var doc = JsonDocument.Parse(raw);
        if (!doc.RootElement.TryGetProperty("access_token", out var token))
            throw new InvalidOperationException("OAuth Inter sem access_token.");
        return token.GetString()
            ?? throw new InvalidOperationException("OAuth Inter access_token vazio.");
    }

    private static string Truncar(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "…";
}

/// <summary>Registra HttpClient com mTLS quando certificado Inter estiver configurado.</summary>
public static class InterPixHttpClientExtensions
{
    public static IHttpClientBuilder AddInterPixHttpClient(
        this IServiceCollection services, InterPixOptions opts)
    {
        return services.AddHttpClient("inter-pix")
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (!string.IsNullOrWhiteSpace(opts.CertificadoPath) && File.Exists(opts.CertificadoPath))
                {
                    var cert = string.IsNullOrEmpty(opts.CertificadoSenha)
                        ? X509CertificateLoader.LoadPkcs12FromFile(opts.CertificadoPath, null)
                        : X509CertificateLoader.LoadPkcs12FromFile(opts.CertificadoPath, opts.CertificadoSenha);
                    handler.ClientCertificates.Add(cert);
                }
                return handler;
            });
    }
}

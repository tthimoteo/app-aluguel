using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Aluguel.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>
/// Adapter SEFIN Nacional: monta DPS → assina A1 → GZIP+Base64 → POST /nfse (mTLS).
/// </summary>
public sealed class SefinNfseNacionalProvider(
    IOptions<NfseOptions> options,
    ILogger<SefinNfseNacionalProvider> logger) : INfseNacionalProvider
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<NfseEmissaoResultado> EmitirAsync(NfseEmissaoPedido pedido, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var opts = options.Value;

        X509Certificate2 cert;
        try
        {
            cert = X509CertificateLoader.LoadPkcs12(
                pedido.CertificadoPfx,
                pedido.SenhaCertificado,
                X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao abrir certificado A1 para emissão NFS-e");
            return Falha("Não foi possível abrir o certificado A1. Verifique o PFX e a senha.");
        }

        using (cert)
        {
            string idDps;
            string xmlAssinado;
            try
            {
                var nDps = DpsXmlBuilder.GerarNumeroDps(pedido.NfseId);
                var emitidoEm = DateTimeOffset.UtcNow;
                var xml = DpsXmlBuilder.Montar(pedido, opts, nDps, emitidoEm, out idDps);
                xmlAssinado = DpsXmlSigner.Assinar(xml, idDps, cert);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao montar/assinar DPS NFS-e");
                return Falha($"Falha ao montar/assinar a DPS: {ex.Message}");
            }

            var payload = JsonSerializer.Serialize(new
            {
                dpsXmlGZipB64 = GzipBase64.CompactarParaBase64(xmlAssinado)
            });

            try
            {
                using var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(cert);
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;

                using var http = new HttpClient(handler)
                {
                    BaseAddress = new Uri(opts.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(90)
                };
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await http.PostAsync("nfse", content, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if ((int)response.StatusCode is >= 200 and < 300)
                {
                    var envelope = JsonSerializer.Deserialize<SefinEmissaoEnvelope>(body, JsonOpts);
                    var chave = envelope?.ChaveAcesso;
                    if (string.IsNullOrWhiteSpace(chave) || string.IsNullOrWhiteSpace(envelope?.NfseXmlGZipB64))
                    {
                        logger.LogWarning("SEFIN retornou HTTP {Status} sem chave/XML. Body: {Body}",
                            (int)response.StatusCode, TruncarLog(body));
                        return Falha("SEFIN aceitou a DPS mas não retornou XML da NFS-e.", xmlAssinado);
                    }

                    byte[] xmlNfse;
                    try
                    {
                        xmlNfse = GzipBase64.DescompactarDeBase64(envelope.NfseXmlGZipB64);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Falha ao descompactar nfseXmlGZipB64");
                        return Falha("Não foi possível descompactar o XML da NFS-e retornado pela SEFIN.", xmlAssinado);
                    }

                    var numero = ExtrairNumero(xmlNfse) ?? ExtrairNumeroDaChave(chave);
                    var serie = ExtrairSerie(xmlNfse) ?? opts.SeriePadrao;

                    logger.LogInformation(
                        "NFS-e emitida na SEFIN. Chave={Chave} IdDps={IdDps} Ambiente={Ambiente}",
                        chave, envelope.IdDps ?? idDps, opts.Ambiente);

                    return new NfseEmissaoResultado(
                        Sucesso: true,
                        Numero: numero,
                        Serie: serie,
                        ChaveAcesso: chave,
                        IdDps: envelope.IdDps ?? idDps,
                        XmlNfse: xmlNfse,
                        XmlDpsEnviado: xmlAssinado,
                        MensagemErro: null);
                }

                var msgErro = ExtrairMensagemErro(body) ?? $"SEFIN HTTP {(int)response.StatusCode}: {TruncarLog(body)}";
                logger.LogWarning("SEFIN rejeitou emissão NFS-e: {Erro}", msgErro);
                return Falha(msgErro, xmlAssinado);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return Falha("Timeout ao chamar a SEFIN Nacional.", xmlAssinado);
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Erro de transporte mTLS/HTTP com a SEFIN");
                return Falha($"Falha de comunicação com a SEFIN: {ex.Message}", xmlAssinado);
            }
        }
    }

    public async Task<NfseCancelamentoResultado> CancelarAsync(
        NfseCancelamentoPedido pedido, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        var opts = options.Value;

        X509Certificate2 cert;
        try
        {
            cert = X509CertificateLoader.LoadPkcs12(
                pedido.CertificadoPfx,
                pedido.SenhaCertificado,
                X509KeyStorageFlags.EphemeralKeySet);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao abrir certificado A1 para cancelamento NFS-e");
            return FalhaCancelamento("Não foi possível abrir o certificado A1. Verifique o PFX e a senha.");
        }

        using (cert)
        {
            string idPedReg;
            string xmlAssinado;
            try
            {
                var xml = EventoCancelamentoXmlBuilder.Montar(
                    pedido, opts, DateTimeOffset.UtcNow, out idPedReg);
                xmlAssinado = DpsXmlSigner.Assinar(xml, idPedReg, cert);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Falha ao montar/assinar evento de cancelamento NFS-e");
                return FalhaCancelamento($"Falha ao montar/assinar o evento de cancelamento: {ex.Message}");
            }

            var chave = new string(pedido.ChaveAcesso.Where(char.IsDigit).ToArray());
            var payload = JsonSerializer.Serialize(new
            {
                pedidoRegistroEventoXmlGZipB64 = GzipBase64.CompactarParaBase64(xmlAssinado)
            });

            try
            {
                using var handler = new HttpClientHandler();
                handler.ClientCertificates.Add(cert);
                handler.ClientCertificateOptions = ClientCertificateOption.Manual;

                using var http = new HttpClient(handler)
                {
                    BaseAddress = new Uri(opts.BaseUrl),
                    Timeout = TimeSpan.FromSeconds(90)
                };
                http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                using var content = new StringContent(payload, Encoding.UTF8, "application/json");
                using var response = await http.PostAsync($"nfse/{chave}/eventos", content, ct);
                var body = await response.Content.ReadAsStringAsync(ct);

                if ((int)response.StatusCode is >= 200 and < 300)
                {
                    var envelope = JsonSerializer.Deserialize<SefinEventoEnvelope>(body, JsonOpts);
                    if (string.IsNullOrWhiteSpace(envelope?.EventoXmlGZipB64))
                    {
                        // Algumas respostas 200 trazem só dataHoraProcessamento (sem XML).
                        logger.LogInformation(
                            "SEFIN aceitou cancelamento sem XML de evento. Chave={Chave} IdPed={Id}",
                            chave, idPedReg);
                        return new NfseCancelamentoResultado(
                            true, idPedReg, Encoding.UTF8.GetBytes(xmlAssinado), null);
                    }

                    byte[] xmlEvento;
                    try
                    {
                        xmlEvento = GzipBase64.DescompactarDeBase64(envelope.EventoXmlGZipB64);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Falha ao descompactar eventoXmlGZipB64");
                        return FalhaCancelamento(
                            "Não foi possível descompactar o XML do evento de cancelamento retornado pela SEFIN.");
                    }

                    var protocolo = ExtrairProtocolo(xmlEvento) ?? idPedReg;
                    logger.LogInformation(
                        "NFS-e cancelada na SEFIN. Chave={Chave} Protocolo={Prot} Ambiente={Ambiente}",
                        chave, protocolo, opts.Ambiente);

                    return new NfseCancelamentoResultado(true, protocolo, xmlEvento, null);
                }

                var msgErro = ExtrairMensagemErro(body)
                              ?? $"SEFIN HTTP {(int)response.StatusCode}: {TruncarLog(body)}";
                logger.LogWarning(
                    "SEFIN rejeitou cancelamento NFS-e: {Erro}. Body={Body}",
                    msgErro, TruncarLog(body));
                return FalhaCancelamento(msgErro);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return FalhaCancelamento("Timeout ao chamar a SEFIN Nacional (cancelamento).");
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Erro de transporte mTLS/HTTP com a SEFIN (cancelamento)");
                return FalhaCancelamento($"Falha de comunicação com a SEFIN: {ex.Message}");
            }
        }
    }

    private static NfseEmissaoResultado Falha(string mensagem, string? xmlDps = null) =>
        new(false, null, null, null, null, null, xmlDps, mensagem);

    private static NfseCancelamentoResultado FalhaCancelamento(string mensagem) =>
        new(false, null, null, mensagem);

    private static string? ExtrairMensagemErro(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // SEFIN usa tanto "erros" quanto "erro" — e "erro" pode ser objeto ou array.
            foreach (var nome in new[] { "erros", "erro" })
            {
                if (!root.TryGetProperty(nome, out var no))
                    continue;

                var partes = ExtrairItensErro(no);
                if (partes.Count > 0)
                    return string.Join("; ", partes);
            }
        }
        catch (JsonException)
        {
            // body não-JSON
        }
        catch (InvalidOperationException)
        {
            // ValueKind inesperado (ex.: primitivo) — cai no fallback HTTP do caller
        }

        return null;
    }

    private static List<string> ExtrairItensErro(JsonElement no)
    {
        var partes = new List<string>();
        if (no.ValueKind == JsonValueKind.Array)
        {
            foreach (var e in no.EnumerateArray())
            {
                var linha = FormatarItemErro(e);
                if (!string.IsNullOrWhiteSpace(linha))
                    partes.Add(linha);
            }
        }
        else if (no.ValueKind == JsonValueKind.Object)
        {
            var linha = FormatarItemErro(no);
            if (!string.IsNullOrWhiteSpace(linha))
                partes.Add(linha);
        }
        else if (no.ValueKind == JsonValueKind.String)
        {
            var s = no.GetString();
            if (!string.IsNullOrWhiteSpace(s))
                partes.Add(s);
        }

        return partes;
    }

    private static string FormatarItemErro(JsonElement e)
    {
        if (e.ValueKind != JsonValueKind.Object)
            return e.ValueKind == JsonValueKind.String ? (e.GetString() ?? "") : "";

        var codigo = Prop(e, "Codigo", "codigo");
        var desc = Prop(e, "Descricao", "descricao") ?? Prop(e, "Message", "message");
        var comp = Prop(e, "Complemento", "complemento");
        return string.Join(" — ", new[] { codigo, desc, comp }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private static string? Prop(JsonElement el, params string[] names)
    {
        if (el.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var n in names)
        {
            if (el.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String)
                return v.GetString();
        }

        return null;
    }

    private static long? ExtrairNumero(byte[] xmlBytes)
    {
        var texto = Encoding.UTF8.GetString(xmlBytes);
        var m = Regex.Match(texto, @"<nNFSe>(\d+)</nNFSe>", RegexOptions.CultureInvariant);
        if (!m.Success)
            m = Regex.Match(texto, @"<Numero>(\d+)</Numero>", RegexOptions.CultureInvariant);
        if (!m.Success) return null;
        return long.TryParse(m.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
            ? n
            : null;
    }

    private static string? ExtrairSerie(byte[] xmlBytes)
    {
        var texto = Encoding.UTF8.GetString(xmlBytes);
        var m = Regex.Match(texto, @"<serie>([^<]+)</serie>", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    /// <summary>Fallback: trecho numérico da chave de acesso (50 pos).</summary>
    private static long ExtrairNumeroDaChave(string chave)
    {
        var digitos = new string(chave.Where(char.IsDigit).ToArray());
        if (digitos.Length >= 9 &&
            long.TryParse(digitos[^9..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) &&
            n > 0)
            return n;
        return Math.Abs(chave.GetHashCode(StringComparison.Ordinal)) % 900_000 + 100_000;
    }

    private static string? ExtrairProtocolo(byte[] xmlBytes)
    {
        var texto = Encoding.UTF8.GetString(xmlBytes);
        var m = Regex.Match(texto, @"<nProt>([^<]+)</nProt>", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static string TruncarLog(string? s) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= 800 ? s : s[..800] + "…";

    private sealed class SefinEmissaoEnvelope
    {
        public string? IdDps { get; set; }
        public string? ChaveAcesso { get; set; }
        public string? NfseXmlGZipB64 { get; set; }
    }

    private sealed class SefinEventoEnvelope
    {
        public string? EventoXmlGZipB64 { get; set; }
    }
}

using System.Net;
using System.Net.Http.Headers;
using Aluguel.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aluguel.Infrastructure.Storage;

/// <summary>
/// Adapter de produção: Supabase Storage via API REST (service_role).
/// Ver Docs/Arquitetura/07-Estrategia-Supabase-Storage.md.
/// </summary>
public sealed class SupabaseFileStorage(
    IHttpClientFactory httpClientFactory,
    IOptions<StorageOptions> options,
    ILogger<SupabaseFileStorage> logger) : IFileStorage
{
    public const string HttpClientName = "SupabaseStorage";

    public async Task<string> UploadAsync(
        string bucket, string path, Stream conteudo, string contentType, CancellationToken ct = default)
    {
        var key = StoragePath.ObjectKey(bucket, path);
        var client = CreateClient();
        using var content = new StreamContent(conteudo);
        content.Headers.ContentType = new MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType);

        // x-upsert=true: sobrescreve se o path já existir (reenvio de certificado/anexo).
        using var req = new HttpRequestMessage(HttpMethod.Post, $"object/{bucket}/{EscapePath(key)}")
        {
            Content = content,
        };
        req.Headers.TryAddWithoutValidation("x-upsert", "true");

        using var res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            logger.LogError("Supabase upload falhou {Status} bucket={Bucket} key={Key}: {Body}",
                (int)res.StatusCode, bucket, key, Truncate(body));
            throw new InvalidOperationException(
                $"Falha ao enviar arquivo para o Storage ({(int)res.StatusCode}). Verifique buckets e credenciais.");
        }

        return StoragePath.LogicalPath(bucket, key);
    }

    public async Task<Stream> DownloadAsync(string bucket, string path, CancellationToken ct = default)
    {
        var key = StoragePath.ObjectKey(bucket, path);
        var client = CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Get, $"object/{bucket}/{EscapePath(key)}");
        var res = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        if (res.StatusCode == HttpStatusCode.NotFound)
        {
            res.Dispose();
            throw new FileNotFoundException($"Arquivo não encontrado no Supabase Storage: {bucket}/{key}");
        }

        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            res.Dispose();
            logger.LogError("Supabase download falhou {Status} bucket={Bucket} key={Key}: {Body}",
                (int)res.StatusCode, bucket, key, Truncate(body));
            throw new InvalidOperationException(
                $"Falha ao baixar arquivo do Storage ({(int)res.StatusCode}).");
        }

        // Bufferiza: o caller usa Seek em vários fluxos; evita prender a conexão HTTP.
        var ms = new MemoryStream();
        using (res)
        await using (var net = await res.Content.ReadAsStreamAsync(ct))
            await net.CopyToAsync(ms, ct);
        ms.Position = 0;
        return ms;
    }

    public async Task DeleteAsync(string bucket, string path, CancellationToken ct = default)
    {
        var key = StoragePath.ObjectKey(bucket, path);
        var client = CreateClient();
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"object/{bucket}/{EscapePath(key)}");
        using var res = await client.SendAsync(req, ct);
        if (res.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.OK or HttpStatusCode.NoContent)
            return;

        var body = await res.Content.ReadAsStringAsync(ct);
        logger.LogWarning("Supabase delete {Status} bucket={Bucket} key={Key}: {Body}",
            (int)res.StatusCode, bucket, key, Truncate(body));
    }

    private HttpClient CreateClient()
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.SupabaseUrl) || string.IsNullOrWhiteSpace(opts.SupabaseServiceRoleKey))
            throw new InvalidOperationException(
                "Storage Supabase não configurado: defina Storage:SupabaseUrl e Storage:SupabaseServiceRoleKey.");

        var client = httpClientFactory.CreateClient(HttpClientName);
        // BaseAddress é configurada no DI; reforça auth a cada chamada.
        if (client.DefaultRequestHeaders.Authorization is null)
        {
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", opts.SupabaseServiceRoleKey);
            client.DefaultRequestHeaders.TryAddWithoutValidation("apikey", opts.SupabaseServiceRoleKey);
        }

        return client;
    }

    /// <summary>Escapa cada segmento do path (não o '/').</summary>
    private static string EscapePath(string key) =>
        string.Join('/', key.Split('/', StringSplitOptions.RemoveEmptyEntries).Select(Uri.EscapeDataString));

    private static string Truncate(string s) =>
        s.Length <= 400 ? s : s[..400] + "…";
}

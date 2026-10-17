using Aluguel.Application.Abstractions;
using Microsoft.Extensions.Configuration;

namespace Aluguel.Infrastructure.Storage;

/// <summary>
/// Adapter de desenvolvimento: grava arquivos em disco sob <c>Storage:LocalRoot</c>
/// (padrão <c>./storage</c>). Em produção, substituir por Supabase Storage (doc 07).
/// </summary>
public sealed class LocalFileStorage(IConfiguration configuration) : IFileStorage
{
    private string Raiz =>
        Path.GetFullPath(configuration["Storage:LocalRoot"] ?? Path.Combine(Directory.GetCurrentDirectory(), "storage"));

    public async Task<string> UploadAsync(string bucket, string path, Stream conteudo, string contentType, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        _ = contentType;

        var relativo = Combinar(bucket, path);
        var fisico = Path.Combine(Raiz, relativo.Replace('/', Path.DirectorySeparatorChar));
        var pasta = Path.GetDirectoryName(fisico);
        if (!string.IsNullOrEmpty(pasta))
            Directory.CreateDirectory(pasta);

        await using var destino = File.Create(fisico);
        await conteudo.CopyToAsync(destino, ct);
        return relativo.Replace('\\', '/');
    }

    public Task<Stream> DownloadAsync(string bucket, string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var fisico = CaminhoFisico(bucket, path);
        if (!File.Exists(fisico))
            throw new FileNotFoundException("Arquivo não encontrado no storage local.", fisico);
        Stream stream = File.OpenRead(fisico);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string bucket, string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var fisico = CaminhoFisico(bucket, path);
        if (File.Exists(fisico))
            File.Delete(fisico);
        return Task.CompletedTask;
    }

    private string CaminhoFisico(string bucket, string path)
    {
        var relativo = Combinar(bucket, path);
        return Path.Combine(Raiz, relativo.Replace('/', Path.DirectorySeparatorChar));
    }

    private static string Combinar(string bucket, string path)
    {
        var limpo = path.Trim().TrimStart('/');
        // Se o path já inclui o bucket (ex.: contracts/tenant/...), não duplica.
        if (limpo.StartsWith(bucket + "/", StringComparison.OrdinalIgnoreCase))
            return limpo;
        return $"{bucket.Trim()}/{limpo}";
    }
}

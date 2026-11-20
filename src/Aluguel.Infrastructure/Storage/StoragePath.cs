namespace Aluguel.Infrastructure.Storage;

/// <summary>Normaliza bucket + path lógico (compatível com o que LocalFileStorage já grava).</summary>
internal static class StoragePath
{
    /// <summary>Path relativo no bucket (sem prefixo do bucket).</summary>
    public static string ObjectKey(string bucket, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(bucket);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var limpo = path.Trim().Replace('\\', '/').TrimStart('/');
        var prefixo = bucket.Trim() + "/";
        if (limpo.StartsWith(prefixo, StringComparison.OrdinalIgnoreCase))
            limpo = limpo[prefixo.Length..];
        if (string.IsNullOrWhiteSpace(limpo))
            throw new ArgumentException("Path do objeto inválido.", nameof(path));
        return limpo;
    }

    /// <summary>Caminho lógico persistido no banco: <c>bucket/…</c>.</summary>
    public static string LogicalPath(string bucket, string objectKey) =>
        $"{bucket.Trim()}/{ObjectKey(bucket, objectKey)}";
}

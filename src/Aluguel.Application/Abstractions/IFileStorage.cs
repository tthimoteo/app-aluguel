namespace Aluguel.Application.Abstractions;

/// <summary>
/// Port de armazenamento de arquivos (Supabase Storage em produção; disco local em desenvolvimento).
/// Ver Docs/Arquitetura/07-Estrategia-Supabase-Storage.md.
/// </summary>
public interface IFileStorage
{
    /// <summary>Faz upload e devolve o caminho lógico gravado (bucket/path).</summary>
    Task<string> UploadAsync(string bucket, string path, Stream conteudo, string contentType, CancellationToken ct = default);

    /// <summary>Abre o arquivo para leitura (uso interno / download mediado pela API).</summary>
    Task<Stream> DownloadAsync(string bucket, string path, CancellationToken ct = default);

    Task DeleteAsync(string bucket, string path, CancellationToken ct = default);
}

namespace Aluguel.Application.Abstractions;

/// <summary>
/// Gera o DANFSe (PDF) a partir do XML autorizado da NFS-e, conforme NT 008/2026.
/// </summary>
public interface INfsePdfGenerator
{
    /// <summary>
    /// Renderiza o DANFSe exclusivamente com dados presentes no XML autorizado.
    /// <paramref name="chaveAcesso"/> e <paramref name="competencia"/> são fallbacks
    /// se o XML não trouxer Id/dCompet.
    /// </summary>
    byte[] Gerar(byte[] xmlNfseAutorizado, string? chaveAcesso = null, string? competencia = null);
}

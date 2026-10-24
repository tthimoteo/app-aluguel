namespace Aluguel.Application.Abstractions;

/// <summary>
/// Gera PDF auxiliar da NFS-e (DANFSe simplificado) a partir do XML autorizado pela SEFIN.
/// </summary>
public interface INfsePdfGenerator
{
    /// <summary>Retorna bytes de um PDF válido abrível em leitores comuns.</summary>
    byte[] Gerar(byte[] xmlNfseAutorizado, string chaveAcesso, string competencia);
}

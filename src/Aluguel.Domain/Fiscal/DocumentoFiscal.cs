using Aluguel.Domain.Common;

namespace Aluguel.Domain.Fiscal;

/// <summary>Arquivo fiscal (XML/PDF/XML de cancelamento) armazenado no Supabase Storage.</summary>
public class DocumentoFiscal : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid NfseId { get; private set; }
    public TipoDocumentoFiscal Tipo { get; private set; }
    public string StoragePath { get; private set; } = default!;
    public string? ContentHash { get; private set; }
    public long? TamanhoBytes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private DocumentoFiscal() { }

    public DocumentoFiscal(Guid tenantId, Guid nfseId, TipoDocumentoFiscal tipo, string storagePath,
        string? contentHash = null, long? tamanhoBytes = null)
    {
        TenantId = tenantId;
        NfseId = nfseId;
        Tipo = tipo;
        StoragePath = storagePath;
        ContentHash = contentHash;
        TamanhoBytes = tamanhoBytes;
    }
}

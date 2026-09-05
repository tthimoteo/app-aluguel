using Aluguel.Domain.Common;

namespace Aluguel.Domain.Fiscal;

/// <summary>Certificado digital A1 do cliente (§5). Senha armazenada criptografada; acesso só no backend.</summary>
public class CertificadoDigital : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public string StoragePath { get; private set; } = default!;   // bucket 'certificates'
    public string Thumbprint { get; private set; } = default!;
    public DateTimeOffset Validade { get; private set; }
    public byte[] SenhaCifrada { get; private set; } = default!;
    public bool Ativo { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private CertificadoDigital() { }

    public CertificadoDigital(Guid tenantId, Guid clienteId, string storagePath, string thumbprint,
        DateTimeOffset validade, byte[] senhaCifrada)
    {
        TenantId = tenantId;
        ClienteId = clienteId;
        StoragePath = storagePath;
        Thumbprint = thumbprint;
        Validade = validade;
        SenhaCifrada = senhaCifrada;
    }
}

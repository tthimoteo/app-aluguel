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
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Caminho do certificado é obrigatório.", nameof(storagePath));
        if (string.IsNullOrWhiteSpace(thumbprint))
            throw new ArgumentException("Thumbprint é obrigatório.", nameof(thumbprint));
        if (senhaCifrada is null || senhaCifrada.Length == 0)
            throw new ArgumentException("Senha cifrada é obrigatória.", nameof(senhaCifrada));

        TenantId = tenantId;
        ClienteId = clienteId;
        StoragePath = storagePath.Trim();
        Thumbprint = thumbprint.Trim();
        Validade = validade;
        SenhaCifrada = senhaCifrada;
        Ativo = true;
    }

    public void Desativar() => Ativo = false;
}

using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using Aluguel.Domain.ValueObjects;

namespace Aluguel.Domain.Inquilinos;

/// <summary>Inquilino (destinatário da NFS-e) associado a um imóvel via contrato (§8).</summary>
public class Inquilino : AggregateRoot, ITenantOwned, IAuditable, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public TipoPessoa TipoPessoa { get; private set; }
    public string Nome { get; private set; } = default!;      // nome ou razão social
    public string Documento { get; private set; } = default!;  // CPF ou CNPJ
    public string? InscricaoMunicipal { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public Endereco Endereco { get; private set; } = Endereco.Vazio();
    public StatusAtivoInativo Status { get; private set; } = StatusAtivoInativo.Ativo;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; private set; }

    private Inquilino() { }

    public Inquilino(Guid tenantId, Guid clienteId, TipoPessoa tipoPessoa, string nome, string documento,
        string? telefone = null, string? email = null, Endereco? endereco = null, string? inscricaoMunicipal = null)
    {
        TenantId = tenantId;
        ClienteId = clienteId;
        TipoPessoa = tipoPessoa;
        Nome = nome;
        Documento = documento;
        Telefone = telefone;
        Email = email;
        Endereco = endereco ?? Endereco.Vazio();
        InscricaoMunicipal = inscricaoMunicipal;
    }
}

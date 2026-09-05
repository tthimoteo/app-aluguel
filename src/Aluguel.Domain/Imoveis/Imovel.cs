using Aluguel.Domain.Common;
using Aluguel.Domain.ValueObjects;

namespace Aluguel.Domain.Imoveis;

/// <summary>Imóvel do cliente disponível para locação (§7).</summary>
public class Imovel : AggregateRoot, ITenantOwned, IAuditable, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public string Nome { get; private set; } = default!;
    public TipoImovel Tipo { get; private set; }
    public Endereco Endereco { get; private set; } = Endereco.Vazio();
    public string? NumeroIptu { get; private set; }
    public string? NumeroMatricula { get; private set; }
    public StatusAtivoInativo Status { get; private set; } = StatusAtivoInativo.Ativo;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DeletedAt { get; private set; }

    private Imovel() { }

    public Imovel(Guid tenantId, Guid clienteId, string nome, TipoImovel tipo, Endereco? endereco = null,
        string? numeroIptu = null, string? numeroMatricula = null)
    {
        TenantId = tenantId;
        ClienteId = clienteId;
        Nome = nome;
        Tipo = tipo;
        Endereco = endereco ?? Endereco.Vazio();
        NumeroIptu = numeroIptu;
        NumeroMatricula = numeroMatricula;
    }
}

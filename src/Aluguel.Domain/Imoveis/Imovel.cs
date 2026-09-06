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
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        TenantId = tenantId;
        ClienteId = clienteId;
        Nome = nome;
        Tipo = tipo;
        Endereco = endereco ?? Endereco.Vazio();
        NumeroIptu = numeroIptu;
        NumeroMatricula = numeroMatricula;
    }

    public void Atualizar(string nome, TipoImovel tipo, Endereco? endereco,
        string? numeroIptu, string? numeroMatricula)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));

        Nome = nome;
        Tipo = tipo;
        if (endereco is not null) Endereco = endereco;
        NumeroIptu = numeroIptu;
        NumeroMatricula = numeroMatricula;
        Touch();
    }

    /// <summary>Inativa o imóvel (ex.: enquadramento em downgrade de plano — CASO 2).</summary>
    public void Inativar()
    {
        Status = StatusAtivoInativo.Inativo;
        Touch();
    }

    public void Ativar()
    {
        Status = StatusAtivoInativo.Ativo;
        Touch();
    }

    /// <summary>Exclusão lógica (soft delete). Só deve ocorrer sem dependentes (§4).</summary>
    public void Remover()
    {
        if (DeletedAt is not null) return;
        DeletedAt = DateTimeOffset.UtcNow;
        Status = StatusAtivoInativo.Inativo;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

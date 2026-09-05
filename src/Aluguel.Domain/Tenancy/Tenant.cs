using Aluguel.Domain.Common;

namespace Aluguel.Domain.Tenancy;

/// <summary>Locatário (tenant) da plataforma multi-tenant.</summary>
public class Tenant : AggregateRoot
{
    public string Nome { get; private set; } = default!;
    public string Subdominio { get; private set; } = default!;
    public bool Ativo { get; private set; } = true;
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Tenant() { }

    public Tenant(string nome, string subdominio)
    {
        Nome = nome;
        Subdominio = subdominio;
    }
}

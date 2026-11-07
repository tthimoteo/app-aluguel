using Aluguel.Domain.Tenancy;

namespace Aluguel.Application.Abstractions;

/// <summary>Porta para provisionar tenants no cadastro público (self-service).</summary>
public interface ITenantStore
{
    Task<bool> SubdominioEmUsoAsync(string subdominio, CancellationToken ct = default);

    void Adicionar(Tenant tenant);

    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
}

using Aluguel.Application.Abstractions;
using Aluguel.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

public sealed class TenantStore(AppDbContext db) : ITenantStore
{
    public Task<bool> SubdominioEmUsoAsync(string subdominio, CancellationToken ct = default) =>
        db.Tenants.AnyAsync(t => t.Subdominio == subdominio, ct);

    public void Adicionar(Tenant tenant) => db.Tenants.Add(tenant);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}

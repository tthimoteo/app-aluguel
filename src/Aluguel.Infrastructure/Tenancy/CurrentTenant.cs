using Aluguel.Application.Abstractions;

namespace Aluguel.Infrastructure.Tenancy;

/// <summary>
/// Tenant nulo (sem filtro) — usado em tempo de design/migrations e enquanto não há autenticação.
/// A resolução real via claim `tenant_id` do JWT será registrada na camada de API (com IHttpContextAccessor)
/// quando os endpoints de autenticação forem implementados.
/// </summary>
public sealed class NullCurrentTenant : ICurrentTenant
{
    public Guid? TenantId => null;
}

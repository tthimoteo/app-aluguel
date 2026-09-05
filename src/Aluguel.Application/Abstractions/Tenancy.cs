namespace Aluguel.Application.Abstractions;

/// <summary>Tenant corrente (resolvido do JWT quando houver autenticação). Usado pelo filtro global do EF.</summary>
public interface ICurrentTenant
{
    Guid? TenantId { get; }
}

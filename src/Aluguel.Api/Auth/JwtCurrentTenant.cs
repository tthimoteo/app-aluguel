using Aluguel.Application.Abstractions;

namespace Aluguel.Api.Auth;

/// <summary>Resolve o tenant a partir da claim `tenant_id` do JWT da requisição corrente.</summary>
public class JwtCurrentTenant(IHttpContextAccessor accessor) : ICurrentTenant
{
    public Guid? TenantId
    {
        get
        {
            var claim = accessor.HttpContext?.User?.FindFirst("tenant_id")?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }
}

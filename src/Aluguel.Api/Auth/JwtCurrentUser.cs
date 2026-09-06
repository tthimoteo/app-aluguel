using System.IdentityModel.Tokens.Jwt;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Autorizacao;

namespace Aluguel.Api.Auth;

/// <summary>Resolve o usuário autenticado a partir das claims do JWT (sub, cliente_id, role).</summary>
public class JwtCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var user = accessor.HttpContext?.User;
            var value = user?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                        ?? user?.FindFirst("sub")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public Guid? ClienteId
    {
        get
        {
            var value = accessor.HttpContext?.User?.FindFirst("cliente_id")?.Value;
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool EhAdministrador =>
        accessor.HttpContext?.User?.FindAll("role").Any(c => c.Value == Perfis.Administrador) ?? false;
}

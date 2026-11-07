using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Autorizacao;

namespace Aluguel.Api.Auth;

/// <summary>Resolve o usuário autenticado a partir das claims do JWT (sub, cliente_id, role, name).</summary>
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

    public string? Nome
    {
        get
        {
            var user = accessor.HttpContext?.User;
            var nome = user?.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                       ?? user?.FindFirst("name")?.Value
                       ?? user?.FindFirst(ClaimTypes.Name)?.Value;
            return string.IsNullOrWhiteSpace(nome) ? null : nome.Trim();
        }
    }

    public bool EhAdministrador =>
        accessor.HttpContext?.User?.FindAll("role").Any(c => c.Value == Perfis.Administrador) ?? false;
}

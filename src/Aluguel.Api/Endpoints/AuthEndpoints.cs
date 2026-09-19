using System.Security.Claims;
using Aluguel.Application.Abstractions;

namespace Aluguel.Api.Endpoints;

public record LoginRequest(string Email, string Senha);
public record RefreshRequest(string RefreshToken);
public record LogoutRequest(string RefreshToken);
public record SelecionarClienteRequest(Guid ClienteId, string RefreshToken);

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/auth").WithTags("Auth");

        grupo.MapPost("/login", async (LoginRequest req, IAuthService auth, HttpContext http, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Senha))
                return Results.BadRequest(new { erro = "E-mail e senha são obrigatórios." });

            var r = await auth.LoginAsync(req.Email, req.Senha, Ip(http), ct);
            return r.Sucesso ? Results.Ok(ToResponse(r.Tokens!)) : Results.Unauthorized();
        })
        .WithName("Login").AllowAnonymous();

        grupo.MapPost("/refresh", async (RefreshRequest req, IAuthService auth, HttpContext http, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return Results.BadRequest(new { erro = "refreshToken é obrigatório." });

            var r = await auth.RefreshAsync(req.RefreshToken, Ip(http), ct);
            return r.Sucesso ? Results.Ok(ToResponse(r.Tokens!)) : Results.Unauthorized();
        })
        .WithName("Refresh").AllowAnonymous();

        grupo.MapPost("/logout", async (LogoutRequest req, IAuthService auth, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(req.RefreshToken))
                return Results.BadRequest(new { erro = "refreshToken é obrigatório." });

            await auth.LogoutAsync(req.RefreshToken, ct);
            return Results.NoContent();
        })
        .WithName("Logout").RequireAuthorization();

        grupo.MapPost("/contexto", async (SelecionarClienteRequest req, IAuthService auth, ICurrentUser current,
            HttpContext http, CancellationToken ct) =>
        {
            if (current.UserId is null)
                return Results.Unauthorized();
            if (req.ClienteId == Guid.Empty || string.IsNullOrWhiteSpace(req.RefreshToken))
                return Results.BadRequest(new { erro = "clienteId e refreshToken são obrigatórios." });

            var r = await auth.SelecionarClienteAsync(current.UserId.Value, req.ClienteId, req.RefreshToken, Ip(http), ct);
            return r.Sucesso ? Results.Ok(ToResponse(r.Tokens!)) : Results.BadRequest(new { erro = r.Erro });
        })
        .WithName("SelecionarCliente").RequireAuthorization();

        grupo.MapGet("/me", async (ClaimsPrincipal user, IAuthService auth, CancellationToken ct) =>
        {
            var idRaw = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            Guid.TryParse(idRaw, out var userId);
            var clientes = userId == Guid.Empty
                ? []
                : await auth.ListarVinculosAsync(userId, ct);

            return Results.Ok(new
            {
                id = idRaw,
                email = user.FindFirstValue("email"),
                nome = user.FindFirstValue("name"),
                tenantId = user.FindFirstValue("tenant_id"),
                clienteId = user.FindFirstValue("cliente_id"),
                roles = user.FindAll("role").Select(c => c.Value).ToArray(),
                clientes,
            });
        })
        .WithName("Me").RequireAuthorization();

        return app;
    }

    private static object ToResponse(TokensAutenticacao t) => new
    {
        accessToken = t.AccessToken,
        accessTokenExpiresAtUtc = t.AccessTokenExpiresAtUtc,
        refreshToken = t.RefreshToken,
        refreshTokenExpiresAtUtc = t.RefreshTokenExpiresAtUtc,
        usuario = new
        {
            t.Usuario.Id,
            t.Usuario.Email,
            t.Usuario.Nome,
            t.Usuario.TenantId,
            t.Usuario.ClienteId,
            t.Usuario.Roles,
            t.Usuario.Clientes,
        },
    };

    private static string? Ip(HttpContext http) => http.Connection.RemoteIpAddress?.ToString();
}

using System.Security.Cryptography;
using Aluguel.Application.Abstractions;
using Aluguel.Domain.Usuarios;
using Aluguel.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Aluguel.Infrastructure.Identity;

public class AuthService(
    UserManager<AppUser> userManager,
    IJwtTokenService jwt,
    AppDbContext db,
    IOptions<JwtOptions> options) : IAuthService
{
    private const string ErroCredenciais = "E-mail ou senha inválidos.";
    private readonly JwtOptions _options = options.Value;

    public async Task<ResultadoAuth> LoginAsync(string email, string senha, string? ip, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
            return ResultadoAuth.Falha(ErroCredenciais);

        if (await userManager.IsLockedOutAsync(user))
            return ResultadoAuth.Falha("Usuário temporariamente bloqueado por excesso de tentativas.");

        if (user.Status != StatusUsuario.Ativo)
            return ResultadoAuth.Falha("Usuário inativo ou bloqueado.");

        if (!await userManager.CheckPasswordAsync(user, senha))
        {
            await userManager.AccessFailedAsync(user);
            return ResultadoAuth.Falha(ErroCredenciais);
        }

        await userManager.ResetAccessFailedCountAsync(user);
        user.UltimoLogin = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var roles = await userManager.GetRolesAsync(user);
        var tokens = await EmitirTokensAsync(user, roles, ip, revogar: null, ct);
        return ResultadoAuth.Ok(tokens);
    }

    public async Task<ResultadoAuth> RefreshAsync(string refreshToken, string? ip, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);
        var atual = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (atual is null || !atual.Ativo)
            return ResultadoAuth.Falha("Refresh token inválido ou expirado.");

        var user = await userManager.FindByIdAsync(atual.UserId.ToString());
        if (user is null || user.Status != StatusUsuario.Ativo)
            return ResultadoAuth.Falha("Usuário inválido para renovação.");

        var roles = await userManager.GetRolesAsync(user);
        var tokens = await EmitirTokensAsync(user, roles, ip, revogar: atual, ct);
        return ResultadoAuth.Ok(tokens);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);
        var atual = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (atual is { Ativo: true })
        {
            atual.RevokedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task<TokensAutenticacao> EmitirTokensAsync(
        AppUser user, IList<string> roles, string? ip, RefreshToken? revogar, CancellationToken ct)
    {
        var access = jwt.CreateAccessToken(user.Id, user.Email!, user.Nome, user.TenantId, user.ClienteId, [.. roles]);

        var (refreshRaw, refreshHash) = GerarRefreshToken();
        var refreshExpira = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpira,
            CreatedByIp = ip,
        });

        if (revogar is not null)
        {
            revogar.RevokedAt = DateTimeOffset.UtcNow;
            revogar.RevokedByIp = ip;
            revogar.ReplacedByTokenHash = refreshHash;
        }

        await db.SaveChangesAsync(ct);

        var usuario = new UsuarioAutenticado(user.Id, user.Email!, user.Nome, user.TenantId, user.ClienteId, [.. roles]);
        return new TokensAutenticacao(access.Token, access.ExpiresAtUtc, refreshRaw, refreshExpira, usuario);
    }

    private static (string raw, string hash) GerarRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var raw = Base64UrlEncode(bytes);
        return (raw, Hash(raw));
    }

    private static string Hash(string value)
    {
        var bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}

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

        var contexto = await ResolverContextoAsync(user, clienteIdPreferido: null, ct);
        if (!contexto.Ok)
            return ResultadoAuth.Falha(contexto.Mensagem!);

        var tokens = await EmitirTokensAsync(user, contexto.ClienteId, contexto.Roles, ip, revogar: null, ct);
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

        var contexto = await ResolverContextoAsync(user, atual.ClienteId, ct);
        if (!contexto.Ok)
            return ResultadoAuth.Falha(contexto.Mensagem!);

        var tokens = await EmitirTokensAsync(user, contexto.ClienteId, contexto.Roles, ip, revogar: atual, ct);
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

    public async Task<ResultadoAuth> SelecionarClienteAsync(Guid userId, Guid clienteId, string refreshToken,
        string? ip, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);
        var atual = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == hash, ct);
        if (atual is null || !atual.Ativo || atual.UserId != userId)
            return ResultadoAuth.Falha("Refresh token inválido ou expirado.");

        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null || user.Status != StatusUsuario.Ativo)
            return ResultadoAuth.Falha("Usuário inválido.");

        var contexto = await ResolverContextoAsync(user, clienteId, ct);
        if (!contexto.Ok)
            return ResultadoAuth.Falha(contexto.Mensagem!);

        var tokens = await EmitirTokensAsync(user, contexto.ClienteId, contexto.Roles, ip, revogar: atual, ct);
        return ResultadoAuth.Ok(tokens);
    }

    public async Task<IReadOnlyList<VinculoClienteDto>> ListarVinculosAsync(Guid userId, CancellationToken ct = default)
    {
        var vinculos = await (
            from v in db.UsuariosClientes.AsNoTracking()
            join c in db.Clientes.AsNoTracking() on v.ClienteId equals c.Id
            where v.UsuarioId == userId && v.Status == StatusUsuario.Ativo
            orderby c.Nome, c.RazaoSocial
            select new { v.ClienteId, c.Nome, c.RazaoSocial, Perfil = v.Perfil, v.Status }
        ).ToListAsync(ct);

        return vinculos
            .Select(x => new VinculoClienteDto(
                x.ClienteId,
                string.IsNullOrWhiteSpace(x.Nome) ? x.RazaoSocial ?? "" : x.Nome!,
                x.Perfil.ToString(),
                x.Status.ToString()))
            .ToList();
    }

    private async Task<ContextoAuth> ResolverContextoAsync(AppUser user, Guid? clienteIdPreferido, CancellationToken ct)
    {
        if (user.Perfil == PerfilUsuario.Administrador || await userManager.IsInRoleAsync(user, "Administrador"))
            return ContextoAuth.Admin();

        var vinculos = await db.UsuariosClientes.AsNoTracking()
            .Where(v => v.UsuarioId == user.Id && v.Status == StatusUsuario.Ativo)
            .ToListAsync(ct);

        if (vinculos.Count == 0)
            return ContextoAuth.Falha("Usuário sem cliente ativo.");

        var escolhido = clienteIdPreferido is { } cid
            ? vinculos.FirstOrDefault(v => v.ClienteId == cid)
            : vinculos.FirstOrDefault(v => v.ClienteId == user.ClienteId) ?? vinculos[0];

        if (escolhido is null)
            return ContextoAuth.Falha("Você não está vinculado a este cliente.");

        return ContextoAuth.Cliente(escolhido.ClienteId, escolhido.Perfil.ToString());
    }

    private async Task<TokensAutenticacao> EmitirTokensAsync(
        AppUser user, Guid? clienteId, IReadOnlyList<string> roles, string? ip, RefreshToken? revogar,
        CancellationToken ct)
    {
        var access = jwt.CreateAccessToken(user.Id, user.Email!, user.Nome, user.TenantId, clienteId, roles);

        var (refreshRaw, refreshHash) = GerarRefreshToken();
        var refreshExpira = DateTimeOffset.UtcNow.AddDays(_options.RefreshTokenDays);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            ClienteId = clienteId,
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

        var clientes = await ListarVinculosAsync(user.Id, ct);
        var usuario = new UsuarioAutenticado(user.Id, user.Email!, user.Nome, user.TenantId, clienteId, roles, clientes);
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

    private readonly record struct ContextoAuth(bool Ok, string? Mensagem, Guid? ClienteId, IReadOnlyList<string> Roles)
    {
        public static ContextoAuth Admin() => new(true, null, null, ["Administrador"]);
        public static ContextoAuth Cliente(Guid clienteId, string perfil) => new(true, null, clienteId, [perfil]);
        public static ContextoAuth Falha(string mensagem) => new(false, mensagem, null, []);
    }
}

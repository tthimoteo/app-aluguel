namespace Aluguel.Application.Abstractions;

/// <summary>Dados do usuário autenticado retornados nas respostas de auth.</summary>
public record UsuarioAutenticado(
    Guid Id,
    string Email,
    string Nome,
    Guid TenantId,
    Guid? ClienteId,
    IReadOnlyList<string> Roles);

/// <summary>Par de tokens (access + refresh) e dados do usuário.</summary>
public record TokensAutenticacao(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAtUtc,
    UsuarioAutenticado Usuario);

/// <summary>Resultado de uma operação de autenticação (login/refresh).</summary>
public record ResultadoAuth(bool Sucesso, string? Erro, TokensAutenticacao? Tokens)
{
    public static ResultadoAuth Ok(TokensAutenticacao tokens) => new(true, null, tokens);
    public static ResultadoAuth Falha(string erro) => new(false, erro, null);
}

/// <summary>Serviço de autenticação (ASP.NET Identity + JWT + refresh tokens rotativos).</summary>
public interface IAuthService
{
    Task<ResultadoAuth> LoginAsync(string email, string senha, string? ip, CancellationToken ct = default);
    Task<ResultadoAuth> RefreshAsync(string refreshToken, string? ip, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);
}

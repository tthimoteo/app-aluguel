namespace Aluguel.Application.Abstractions;

public record AccessToken(string Token, DateTimeOffset ExpiresAtUtc);

/// <summary>Emite o access token JWT com as claims multi-tenant e as roles do usuário.</summary>
public interface IJwtTokenService
{
    AccessToken CreateAccessToken(
        Guid userId,
        string email,
        string nome,
        Guid tenantId,
        Guid? clienteId,
        IReadOnlyCollection<string> roles);
}

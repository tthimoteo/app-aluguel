namespace Aluguel.Infrastructure.Identity;

/// <summary>
/// Refresh token persistido e rotativo (revogável). Guarda-se apenas o hash SHA-256 do token,
/// nunca o valor em claro. Logout e refresh revogam o token corrente.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string? CreatedByIp { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public bool Ativo => RevokedAt is null && DateTimeOffset.UtcNow < ExpiresAt;
}

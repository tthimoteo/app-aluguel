namespace Aluguel.Infrastructure.Identity;

/// <summary>Configuração do JWT (seção "Jwt" do appsettings / variáveis de ambiente / secrets).</summary>
public class JwtOptions
{
    public const string Section = "Jwt";

    public string Issuer { get; set; } = "aluguel";
    public string Audience { get; set; } = "aluguel";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}

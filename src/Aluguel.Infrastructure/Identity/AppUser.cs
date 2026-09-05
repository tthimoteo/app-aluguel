using Aluguel.Domain.Usuarios;
using Microsoft.AspNetCore.Identity;

namespace Aluguel.Infrastructure.Identity;

/// <summary>Usuário do cliente (§6) modelado como IdentityUser estendido. Senha = PasswordHash.</summary>
public class AppUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public Guid? ClienteId { get; set; }
    public string Nome { get; set; } = default!;
    public string? Cpf { get; set; }
    public string? Telefone { get; set; }
    public PerfilUsuario Perfil { get; set; } = PerfilUsuario.Gestor;
    public StatusUsuario Status { get; set; } = StatusUsuario.Ativo;
    public DateTimeOffset? UltimoLogin { get; set; }
}

public class AppRole : IdentityRole<Guid> { }

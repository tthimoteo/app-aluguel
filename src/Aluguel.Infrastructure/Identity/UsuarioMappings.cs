using Aluguel.Application.Usuarios;

namespace Aluguel.Infrastructure.Identity;

internal static class UsuarioMappings
{
    public static UsuarioDto ParaDto(this AppUser u) => new(
        u.Id,
        u.TenantId,
        u.ClienteId,
        u.Nome,
        u.Email ?? string.Empty,
        u.Cpf,
        u.Telefone,
        u.Perfil.ToString(),
        u.Status.ToString(),
        u.UltimoLogin);
}

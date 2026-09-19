using Aluguel.Application.Usuarios;
using Aluguel.Domain.Usuarios;

namespace Aluguel.Infrastructure.Identity;

internal static class UsuarioMappings
{
    public static UsuarioDto ParaDto(this AppUser u, UsuarioCliente vinculo) => new(
        u.Id,
        u.TenantId,
        vinculo.ClienteId,
        u.Nome,
        u.Email ?? string.Empty,
        u.Cpf,
        u.Telefone,
        vinculo.Perfil.ToString(),
        vinculo.Status.ToString(),
        u.UltimoLogin);

    public static UsuarioDto ParaDtoAdministrador(this AppUser u) => new(
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

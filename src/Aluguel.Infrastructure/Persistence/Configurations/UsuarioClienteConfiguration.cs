using Aluguel.Domain.Clientes;
using Aluguel.Domain.Usuarios;
using Aluguel.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aluguel.Infrastructure.Persistence.Configurations;

public class UsuarioClienteConfiguration : IEntityTypeConfiguration<UsuarioCliente>
{
    public void Configure(EntityTypeBuilder<UsuarioCliente> b)
    {
        b.ToTable("usuario_cliente");
        b.HasKey(x => x.Id);
        b.Property(x => x.Perfil).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        b.HasOne<AppUser>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);

        // Um mesmo usuário não pode ser cadastrado duas vezes no mesmo cliente.
        b.HasIndex(x => new { x.ClienteId, x.UsuarioId }).IsUnique();
        b.HasIndex(x => x.UsuarioId);
        b.HasIndex(x => new { x.ClienteId, x.Status });
    }
}

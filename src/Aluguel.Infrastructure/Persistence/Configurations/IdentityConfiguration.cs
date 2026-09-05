using Aluguel.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aluguel.Infrastructure.Persistence.Configurations;

/// <summary>Mapeia as tabelas do ASP.NET Identity no schema `identity` e as colunas de negócio de AppUser.</summary>
public class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> b)
    {
        b.ToTable("asp_net_users", "identity");
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Cpf).HasMaxLength(11);
        b.Property(x => x.Telefone).HasMaxLength(20);
        b.Property(x => x.Perfil).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        // Usuário pertence a um cliente (§6)
        b.HasOne<Aluguel.Domain.Clientes.Cliente>().WithMany().HasForeignKey(x => x.ClienteId)
            .OnDelete(DeleteBehavior.Restrict);
        // CASO 1: CPF único por cliente
        b.HasIndex(x => new { x.ClienteId, x.Cpf }).IsUnique().HasFilter("cpf IS NOT NULL");
    }
}

public class AppRoleConfiguration : IEntityTypeConfiguration<AppRole>
{
    public void Configure(EntityTypeBuilder<AppRole> b) => b.ToTable("asp_net_roles", "identity");
}

public class IdentityUserRoleConfiguration : IEntityTypeConfiguration<IdentityUserRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<Guid>> b) => b.ToTable("asp_net_user_roles", "identity");
}

public class IdentityUserClaimConfiguration : IEntityTypeConfiguration<IdentityUserClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<Guid>> b) => b.ToTable("asp_net_user_claims", "identity");
}

public class IdentityUserLoginConfiguration : IEntityTypeConfiguration<IdentityUserLogin<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<Guid>> b) => b.ToTable("asp_net_user_logins", "identity");
}

public class IdentityUserTokenConfiguration : IEntityTypeConfiguration<IdentityUserToken<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<Guid>> b) => b.ToTable("asp_net_user_tokens", "identity");
}

public class IdentityRoleClaimConfiguration : IEntityTypeConfiguration<IdentityRoleClaim<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRoleClaim<Guid>> b) => b.ToTable("asp_net_role_claims", "identity");
}

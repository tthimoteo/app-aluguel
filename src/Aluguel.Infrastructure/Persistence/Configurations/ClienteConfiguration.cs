using Aluguel.Domain.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aluguel.Infrastructure.Persistence.Configurations;

public class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> b)
    {
        b.ToTable("cliente");
        b.HasKey(x => x.Id);

        b.Property(x => x.TipoPessoa).HasConversion<string>().HasMaxLength(2).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Nome).HasMaxLength(150);
        b.Property(x => x.Cpf).HasMaxLength(11);
        b.Property(x => x.RazaoSocial).HasMaxLength(150);
        b.Property(x => x.NomeFantasia).HasMaxLength(150);
        b.Property(x => x.Cnpj).HasMaxLength(14);
        b.Property(x => x.InscricaoMunicipal).HasMaxLength(30);
        b.Property(x => x.CnaePrincipal).HasMaxLength(10);
        b.Property(x => x.Telefone).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(150);

        b.OwnsOne(x => x.Endereco, e =>
        {
            e.Property(p => p.Logradouro).HasColumnName("logradouro").HasMaxLength(150);
            e.Property(p => p.Numero).HasColumnName("numero").HasMaxLength(15);
            e.Property(p => p.Complemento).HasColumnName("complemento").HasMaxLength(60);
            e.Property(p => p.Bairro).HasColumnName("bairro").HasMaxLength(80);
            e.Property(p => p.Cidade).HasColumnName("cidade").HasMaxLength(80);
            e.Property(p => p.Uf).HasColumnName("uf").HasMaxLength(2);
            e.Property(p => p.Cep).HasColumnName("cep").HasMaxLength(8);
        });

        b.HasOne<Domain.Tenancy.Tenant>().WithMany().HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Domain.Assinaturas.Plano>().WithMany().HasForeignKey(x => x.PlanoId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => new { x.TenantId, x.Cpf }).IsUnique()
            .HasFilter("cpf IS NOT NULL AND deleted_at IS NULL");
        b.HasIndex(x => new { x.TenantId, x.Cnpj }).IsUnique()
            .HasFilter("cnpj IS NOT NULL AND deleted_at IS NULL");
    }
}

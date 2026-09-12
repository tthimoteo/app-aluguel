using Aluguel.Domain.Clientes;
using Aluguel.Domain.Imoveis;
using Aluguel.Domain.Inquilinos;
using Aluguel.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aluguel.Infrastructure.Persistence.Configurations;

internal static class EnderecoMap
{
    public static void MapEndereco<T>(this OwnedNavigationBuilder<T, Endereco> e) where T : class
    {
        e.Property(p => p.Logradouro).HasColumnName("logradouro").HasMaxLength(150);
        e.Property(p => p.Numero).HasColumnName("numero").HasMaxLength(15);
        e.Property(p => p.Complemento).HasColumnName("complemento").HasMaxLength(60);
        e.Property(p => p.Bairro).HasColumnName("bairro").HasMaxLength(80);
        e.Property(p => p.Cidade).HasColumnName("cidade").HasMaxLength(80);
        e.Property(p => p.Uf).HasColumnName("uf").HasMaxLength(2);
        e.Property(p => p.Cep).HasColumnName("cep").HasMaxLength(8);
    }
}

public class ImovelConfiguration : IEntityTypeConfiguration<Imovel>
{
    public void Configure(EntityTypeBuilder<Imovel> b)
    {
        b.ToTable("imovel");
        b.HasKey(x => x.Id);
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(x => x.NumeroIptu).HasMaxLength(30);
        b.Property(x => x.NumeroMatricula).HasMaxLength(30);
        b.OwnsOne(x => x.Endereco, e => e.MapEndereco());

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ClienteId);
    }
}

public class InquilinoConfiguration : IEntityTypeConfiguration<Inquilino>
{
    public void Configure(EntityTypeBuilder<Inquilino> b)
    {
        b.ToTable("inquilino");
        b.HasKey(x => x.Id);
        b.Property(x => x.TipoPessoa).HasConversion<string>().HasMaxLength(2).IsRequired();
        b.Property(x => x.Nome).HasMaxLength(150).IsRequired();
        b.Property(x => x.Documento).HasMaxLength(14).IsRequired();
        b.Property(x => x.InscricaoMunicipal).HasMaxLength(30);
        b.Property(x => x.Telefone).HasMaxLength(20);
        b.Property(x => x.Email).HasMaxLength(150);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.OwnsOne(x => x.Endereco, e => e.MapEndereco());

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ClienteId);
        b.HasOne<Imovel>().WithMany().HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Restrict);
        // Um inquilino ativo por imóvel (§7 / CASO 3). Soft-deleted e inativos ficam de fora.
        b.HasIndex(x => x.ImovelId)
            .IsUnique()
            .HasFilter("imovel_id IS NOT NULL AND deleted_at IS NULL AND status = 'Ativo'")
            .HasDatabaseName("ix_inquilino_imovel_id_ativo");
    }
}

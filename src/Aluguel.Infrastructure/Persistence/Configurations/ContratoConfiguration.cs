using Aluguel.Domain.Clientes;
using Aluguel.Domain.Contratos;
using Aluguel.Domain.Imoveis;
using Aluguel.Domain.Inquilinos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aluguel.Infrastructure.Persistence.Configurations;

public class ContratoConfiguration : IEntityTypeConfiguration<Contrato>
{
    public void Configure(EntityTypeBuilder<Contrato> b)
    {
        b.ToTable("contrato");
        b.HasKey(x => x.Id);
        b.Property(x => x.NumeroContrato).HasMaxLength(40).IsRequired();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(12).IsRequired();
        b.Property(x => x.ValorAluguel).HasColumnType("numeric(14,2)");
        b.Property(x => x.JurosAtrasoPct).HasColumnType("numeric(6,3)");
        b.Property(x => x.MultaAtrasoPct).HasColumnType("numeric(6,3)");

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Imovel>().WithMany().HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Inquilino>().WithMany().HasForeignKey(x => x.InquilinoId).OnDelete(DeleteBehavior.Restrict);

        // CASO 3: apenas um contrato Ativo por imóvel
        b.HasIndex(x => x.ImovelId).IsUnique()
            .HasFilter("status = 'Ativo'")
            .HasDatabaseName("ux_contrato_imovel_ativo");
        b.HasIndex(x => x.InquilinoId);
    }
}

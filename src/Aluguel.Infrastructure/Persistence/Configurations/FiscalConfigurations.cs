using Aluguel.Domain.Clientes;
using Aluguel.Domain.Contratos;
using Aluguel.Domain.Fiscal;
using Aluguel.Domain.Imoveis;
using Aluguel.Domain.Inquilinos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aluguel.Infrastructure.Persistence.Configurations;

public class NotaFiscalServicoConfiguration : IEntityTypeConfiguration<NotaFiscalServico>
{
    public void Configure(EntityTypeBuilder<NotaFiscalServico> b)
    {
        b.ToTable("nota_fiscal_servico");
        b.HasKey(x => x.Id);
        b.Property(x => x.Competencia).HasColumnType("char(7)").IsRequired();
        b.Property(x => x.Serie).HasMaxLength(5);
        b.Property(x => x.ChaveAcesso).HasMaxLength(50);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(25).IsRequired();
        b.Property(x => x.ValorServico).HasColumnType("numeric(14,2)");
        b.Property(x => x.Desconto).HasColumnType("numeric(14,2)");
        b.Property(x => x.Multa).HasColumnType("numeric(14,2)");
        b.Property(x => x.Juros).HasColumnType("numeric(14,2)");
        b.Property(x => x.ValorFaturado).HasColumnType("numeric(14,2)");
        b.Property(x => x.ProtocoloCancelamento).HasMaxLength(60);

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Imovel>().WithMany().HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Inquilino>().WithMany().HasForeignKey(x => x.InquilinoId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Documentos).WithOne().HasForeignKey(x => x.NfseId).OnDelete(DeleteBehavior.Cascade);

        // CASO 6/7/8: só 1 faturamento não-cancelado por imóvel/competência
        b.HasIndex(x => new { x.ImovelId, x.Competencia }).IsUnique()
            .HasFilter("status <> 'Cancelada'")
            .HasDatabaseName("ux_nfse_competencia_ativa");
        b.HasIndex(x => new { x.ClienteId, x.Status });
    }
}

public class DocumentoFiscalConfiguration : IEntityTypeConfiguration<DocumentoFiscal>
{
    public void Configure(EntityTypeBuilder<DocumentoFiscal> b)
    {
        b.ToTable("documento_fiscal");
        b.HasKey(x => x.Id);
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.StoragePath).IsRequired();
        b.Property(x => x.ContentHash).HasMaxLength(64);
        b.HasIndex(x => x.NfseId);
    }
}

public class CertificadoDigitalConfiguration : IEntityTypeConfiguration<CertificadoDigital>
{
    public void Configure(EntityTypeBuilder<CertificadoDigital> b)
    {
        b.ToTable("certificado_digital");
        b.HasKey(x => x.Id);
        b.Property(x => x.StoragePath).IsRequired();
        b.Property(x => x.Thumbprint).HasMaxLength(80).IsRequired();
        b.Property(x => x.SenhaCifrada).IsRequired();

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ClienteId).HasFilter("ativo");
    }
}

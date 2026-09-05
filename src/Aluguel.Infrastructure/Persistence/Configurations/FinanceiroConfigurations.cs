using Aluguel.Domain.Clientes;
using Aluguel.Domain.Contratos;
using Aluguel.Domain.Financeiro;
using Aluguel.Domain.Fiscal;
using Aluguel.Domain.Imoveis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aluguel.Infrastructure.Persistence.Configurations;

public class PagamentoConfiguration : IEntityTypeConfiguration<Pagamento>
{
    public void Configure(EntityTypeBuilder<Pagamento> b)
    {
        b.ToTable("pagamento");
        b.HasKey(x => x.Id);
        b.Property(x => x.Competencia).HasColumnType("char(7)").IsRequired();
        b.Property(x => x.ValorPago).HasColumnType("numeric(14,2)");

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Imovel>().WithMany().HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Contrato>().WithMany().HasForeignKey(x => x.ContratoId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<NotaFiscalServico>().WithMany().HasForeignKey(x => x.NfseId).OnDelete(DeleteBehavior.Restrict);

        // CASO 6: pagamento único por imóvel/competência
        b.HasIndex(x => new { x.ImovelId, x.Competencia }).IsUnique()
            .HasDatabaseName("ux_pagamento_competencia");
    }
}

public class DespesaConfiguration : IEntityTypeConfiguration<Despesa>
{
    public void Configure(EntityTypeBuilder<Despesa> b)
    {
        b.ToTable("despesa");
        b.HasKey(x => x.Id);
        b.Property(x => x.Tipo).HasConversion<string>().HasMaxLength(10).IsRequired();
        b.Property(x => x.Competencia).HasColumnType("char(7)").IsRequired();
        b.Property(x => x.Categoria).HasMaxLength(40);
        b.Property(x => x.Fornecedor).HasMaxLength(150);
        b.Property(x => x.Valor).HasColumnType("numeric(14,2)");

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Imovel>().WithMany().HasForeignKey(x => x.ImovelId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ImovelId, x.Competencia });
    }
}

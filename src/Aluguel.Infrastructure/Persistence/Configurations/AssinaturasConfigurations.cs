using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Clientes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aluguel.Infrastructure.Persistence.Configurations;

public class AssinaturaConfiguration : IEntityTypeConfiguration<Assinatura>
{
    public void Configure(EntityTypeBuilder<Assinatura> b)
    {
        b.ToTable("assinatura");
        b.HasKey(x => x.Id);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.ValorMensal).HasColumnType("numeric(14,2)");
        b.Property(x => x.MercadoPagoSubscriptionId).HasMaxLength(60);

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Plano>().WithMany().HasForeignKey(x => x.PlanoId).OnDelete(DeleteBehavior.Restrict);
        b.HasMany(x => x.Pagamentos).WithOne().HasForeignKey(x => x.AssinaturaId).OnDelete(DeleteBehavior.Cascade);

        b.HasIndex(x => x.ClienteId)
            .HasFilter("status IN ('Trial','PendentePagamento','Ativa')")
            .IsUnique()
            .HasDatabaseName("ux_assinatura_cliente_vigente");
        b.HasIndex(x => x.ProximaCobranca).HasFilter("status = 'Ativa'");
    }
}

public class PagamentoPlanoConfiguration : IEntityTypeConfiguration<PagamentoPlano>
{
    public void Configure(EntityTypeBuilder<PagamentoPlano> b)
    {
        b.ToTable("pagamento_plano");
        b.HasKey(x => x.Id);
        b.Property(x => x.Valor).HasColumnType("numeric(14,2)");
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.MetodoPagamento).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.MercadoPagoPaymentId).HasMaxLength(60);

        b.HasIndex(x => x.MercadoPagoPaymentId).IsUnique()
            .HasFilter("mercado_pago_payment_id IS NOT NULL");
    }
}

public class AuditoriaAssinaturaConfiguration : IEntityTypeConfiguration<AuditoriaAssinatura>
{
    public void Configure(EntityTypeBuilder<AuditoriaAssinatura> b)
    {
        b.ToTable("auditoria_assinatura");
        b.HasKey(x => x.Id);
        b.Property(x => x.Evento).HasConversion<string>().HasMaxLength(40).IsRequired();
        b.Property(x => x.PlanoAnterior).HasMaxLength(20);
        b.Property(x => x.NovoPlano).HasMaxLength(20);
        b.Property(x => x.Valor).HasColumnType("numeric(14,2)");

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ClienteId, x.DataHora });
    }
}

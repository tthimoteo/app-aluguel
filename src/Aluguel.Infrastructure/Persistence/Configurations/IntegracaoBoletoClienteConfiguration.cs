using Aluguel.Domain.Clientes;
using Aluguel.Domain.Financeiro;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aluguel.Infrastructure.Persistence.Configurations;

public class IntegracaoBoletoClienteConfiguration : IEntityTypeConfiguration<IntegracaoBoletoCliente>
{
    public void Configure(EntityTypeBuilder<IntegracaoBoletoCliente> b)
    {
        b.ToTable("integracao_boleto_cliente");
        b.HasKey(x => x.Id);
        b.Property(x => x.ProvedorCodigo).HasMaxLength(40).IsRequired();
        b.Property(x => x.ConfiguracaoPublicaJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.SegredosCifrados).IsRequired();

        b.HasOne<Cliente>().WithMany().HasForeignKey(x => x.ClienteId).OnDelete(DeleteBehavior.Restrict);

        // Uma integração ativa por cliente
        b.HasIndex(x => x.ClienteId)
            .IsUnique()
            .HasFilter("ativo")
            .HasDatabaseName("ux_integracao_boleto_cliente_ativo");
        b.HasIndex(x => x.ProvedorCodigo);
    }
}

using Aluguel.Domain.Auditoria;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aluguel.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("audit_log");
        b.HasKey(x => x.Id);
        b.Property(x => x.Acao).HasMaxLength(40).IsRequired();
        b.Property(x => x.Tabela).HasMaxLength(60);
        b.Property(x => x.ValoresAntes).HasColumnType("jsonb");
        b.Property(x => x.ValoresDepois).HasColumnType("jsonb");
        b.Property(x => x.Ip).HasMaxLength(60);
        b.HasIndex(x => new { x.TenantId, x.Tabela, x.RegistroId });
    }
}

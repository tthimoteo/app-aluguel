using Aluguel.Domain.Assinaturas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aluguel.Infrastructure.Persistence.Configurations;

public class PlanoConfiguration : IEntityTypeConfiguration<Plano>
{
    // Data fixa para determinismo das migrations/seed.
    private static readonly DateTimeOffset Seed = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void Configure(EntityTypeBuilder<Plano> b)
    {
        b.ToTable("plano");
        b.HasKey(x => x.Id);
        b.Property(x => x.Codigo).HasMaxLength(20).IsRequired();
        b.HasIndex(x => x.Codigo).IsUnique();
        b.Property(x => x.Nome).HasMaxLength(60).IsRequired();
        b.Property(x => x.ValorMensal).HasColumnType("numeric(14,2)");

        // Seed via objetos anônimos com valores fixos (determinístico para migrations).
        b.HasData(
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111101"), Codigo = PlanoCodigo.Trial, Nome = "Trial", MaxImoveis = (int?)1, MaxUsuarios = (int?)1, PermiteNfse = false, TrialDias = 7, ValorMensal = (decimal?)0m, Ativo = true, CreatedAt = Seed, UpdatedAt = Seed },
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111102"), Codigo = PlanoCodigo.Basico, Nome = "Básico", MaxImoveis = (int?)3, MaxUsuarios = (int?)1, PermiteNfse = false, TrialDias = 0, ValorMensal = (decimal?)49.99m, Ativo = true, CreatedAt = Seed, UpdatedAt = Seed },
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111103"), Codigo = PlanoCodigo.Intermediario, Nome = "Intermediário", MaxImoveis = (int?)5, MaxUsuarios = (int?)3, PermiteNfse = true, TrialDias = 0, ValorMensal = (decimal?)149.99m, Ativo = true, CreatedAt = Seed, UpdatedAt = Seed },
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111104"), Codigo = PlanoCodigo.Avancado, Nome = "Avançado", MaxImoveis = (int?)10, MaxUsuarios = (int?)5, PermiteNfse = true, TrialDias = 0, ValorMensal = (decimal?)249.99m, Ativo = true, CreatedAt = Seed, UpdatedAt = Seed },
            new { Id = Guid.Parse("11111111-1111-1111-1111-111111111105"), Codigo = PlanoCodigo.Pro, Nome = "Pro", MaxImoveis = (int?)null, MaxUsuarios = (int?)null, PermiteNfse = true, TrialDias = 0, ValorMensal = (decimal?)null, Ativo = true, CreatedAt = Seed, UpdatedAt = Seed });
    }
}

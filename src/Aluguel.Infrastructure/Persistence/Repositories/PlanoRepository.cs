using Aluguel.Application.Abstractions;
using Aluguel.Domain.Assinaturas;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

public class PlanoRepository(AppDbContext db) : IPlanoRepository
{
    public async Task<IReadOnlyList<Plano>> ListarAtivosAsync(CancellationToken ct = default) =>
        await db.Planos.AsNoTracking()
            .Where(p => p.Ativo)
            .OrderBy(p => p.ValorMensal ?? decimal.MaxValue)
            .ToListAsync(ct);
}

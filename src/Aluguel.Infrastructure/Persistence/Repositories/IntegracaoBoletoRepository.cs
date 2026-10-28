using Aluguel.Application.Abstractions;
using Aluguel.Domain.Financeiro;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

public sealed class IntegracaoBoletoRepository(AppDbContext db) : IIntegracaoBoletoRepository
{
    public Task<IntegracaoBoletoCliente?> ObterAtivaPorClienteAsync(Guid clienteId, CancellationToken ct = default) =>
        db.Set<IntegracaoBoletoCliente>()
            .FirstOrDefaultAsync(i => i.ClienteId == clienteId && i.Ativo, ct);

    public void Adicionar(IntegracaoBoletoCliente integracao) =>
        db.Set<IntegracaoBoletoCliente>().Add(integracao);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

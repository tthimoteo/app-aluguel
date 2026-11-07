using Aluguel.Application.Abstractions;
using Aluguel.Domain.Assinaturas;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

public class AssinaturaRepository(AppDbContext db) : IAssinaturaRepository
{
    private static readonly StatusAssinatura[] Vigentes =
    [
        StatusAssinatura.Trial,
        StatusAssinatura.PendentePagamento,
        StatusAssinatura.Ativa,
    ];

    public Task<Assinatura?> ObterVigentePorClienteAsync(Guid clienteId, CancellationToken ct = default) =>
        db.Assinaturas
            .Include(a => a.Pagamentos)
            .FirstOrDefaultAsync(a => a.ClienteId == clienteId && Vigentes.Contains(a.Status), ct);

    public Task<Assinatura?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Assinaturas.Include(a => a.Pagamentos).FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<PagamentoPlano?> ObterPagamentoPorIdAsync(Guid pagamentoId, CancellationToken ct = default) =>
        db.PagamentosPlano.FirstOrDefaultAsync(p => p.Id == pagamentoId, ct);

    public Task<PagamentoPlano?> ObterPagamentoPorTxIdAsync(string pixTxId, CancellationToken ct = default) =>
        db.PagamentosPlano.FirstOrDefaultAsync(p => p.PixTxId == pixTxId, ct);

    public async Task<Assinatura?> ObterPorPagamentoIdAsync(Guid pagamentoId, CancellationToken ct = default)
    {
        var pagamento = await db.PagamentosPlano.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == pagamentoId, ct);
        if (pagamento is null) return null;

        return await db.Assinaturas
            .Include(a => a.Pagamentos)
            .FirstOrDefaultAsync(a => a.Id == pagamento.AssinaturaId, ct);
    }

    public void Adicionar(Assinatura assinatura) => db.Assinaturas.Add(assinatura);

    public void AdicionarPagamento(PagamentoPlano pagamento) => db.PagamentosPlano.Add(pagamento);

    public void AdicionarAuditoria(AuditoriaAssinatura auditoria) => db.AuditoriasAssinatura.Add(auditoria);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) =>
        db.SaveChangesAsync(ct);
}

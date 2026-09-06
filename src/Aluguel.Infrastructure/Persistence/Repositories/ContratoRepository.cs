using Aluguel.Application.Abstractions;
using Aluguel.Domain.Contratos;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

/// <summary>
/// As consultas usam <c>db.Contratos</c>, que já aplica o filtro global de tenant —
/// garantindo o isolamento multi-tenant automaticamente.
/// </summary>
public class ContratoRepository(AppDbContext db) : IContratoRepository
{
    public Task<Contrato?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Contratos.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Contrato>> ListarAsync(Guid? clienteId, Guid? imovelId, Guid? inquilinoId,
        StatusContrato? status, int skip, int take, CancellationToken ct = default) =>
        await Filtrar(clienteId, imovelId, inquilinoId, status)
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> ContarAsync(Guid? clienteId, Guid? imovelId, Guid? inquilinoId,
        StatusContrato? status, CancellationToken ct = default) =>
        Filtrar(clienteId, imovelId, inquilinoId, status).CountAsync(ct);

    public Task<bool> ImovelPossuiContratoAtivoAsync(Guid imovelId, Guid? ignorarId, CancellationToken ct = default) =>
        db.Contratos.AnyAsync(c => c.ImovelId == imovelId && c.Status == StatusContrato.Ativo
            && (ignorarId == null || c.Id != ignorarId), ct);

    public void Adicionar(Contrato contrato) => db.Contratos.Add(contrato);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private IQueryable<Contrato> Filtrar(Guid? clienteId, Guid? imovelId, Guid? inquilinoId, StatusContrato? status)
    {
        var query = db.Contratos.AsQueryable();

        if (clienteId is { } cid)
            query = query.Where(c => c.ClienteId == cid);

        if (imovelId is { } iid)
            query = query.Where(c => c.ImovelId == iid);

        if (inquilinoId is { } inid)
            query = query.Where(c => c.InquilinoId == inid);

        if (status is { } s)
            query = query.Where(c => c.Status == s);

        return query;
    }
}

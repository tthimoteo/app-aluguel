using Aluguel.Application.Abstractions;
using Aluguel.Domain.Common;
using Aluguel.Domain.Contratos;
using Aluguel.Domain.Imoveis;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

/// <summary>
/// As consultas usam <c>db.Imoveis</c>, que já aplica os filtros globais de tenant e soft delete —
/// garantindo o isolamento multi-tenant automaticamente.
/// </summary>
public class ImovelRepository(AppDbContext db) : IImovelRepository
{
    public Task<Imovel?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Imoveis.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Imovel>> ListarAsync(Guid? clienteId, string? termo, TipoImovel? tipo,
        StatusAtivoInativo? status, int skip, int take, CancellationToken ct = default) =>
        await Filtrar(clienteId, termo, tipo, status)
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> ContarAsync(Guid? clienteId, string? termo, TipoImovel? tipo,
        StatusAtivoInativo? status, CancellationToken ct = default) =>
        Filtrar(clienteId, termo, tipo, status).CountAsync(ct);

    public async Task<bool> PodeAdicionarImovelAsync(Guid clienteId, CancellationToken ct = default)
    {
        var planoId = await db.Clientes
            .Where(c => c.Id == clienteId)
            .Select(c => c.PlanoId)
            .FirstOrDefaultAsync(ct);

        if (planoId is null)
            return false;

        var plano = await db.Planos.FirstOrDefaultAsync(p => p.Id == planoId, ct);
        if (plano is null)
            return false;

        var ativos = await db.Imoveis
            .CountAsync(i => i.ClienteId == clienteId && i.Status == StatusAtivoInativo.Ativo, ct);

        return plano.PermiteMaisImoveis(ativos);
    }

    public Task<bool> PossuiContratoAtivoAsync(Guid imovelId, CancellationToken ct = default) =>
        db.Contratos.AnyAsync(c => c.ImovelId == imovelId && c.Status == StatusContrato.Ativo, ct);

    public void Adicionar(Imovel imovel) => db.Imoveis.Add(imovel);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private IQueryable<Imovel> Filtrar(Guid? clienteId, string? termo, TipoImovel? tipo, StatusAtivoInativo? status)
    {
        var query = db.Imoveis.AsQueryable();

        if (clienteId is { } cid)
            query = query.Where(i => i.ClienteId == cid);

        if (tipo is { } t)
            query = query.Where(i => i.Tipo == t);

        if (status is { } s)
            query = query.Where(i => i.Status == s);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var padrao = $"%{termo.Trim()}%";
            query = query.Where(i =>
                EF.Functions.ILike(i.Nome, padrao) ||
                (i.NumeroIptu != null && EF.Functions.ILike(i.NumeroIptu, padrao)) ||
                (i.NumeroMatricula != null && EF.Functions.ILike(i.NumeroMatricula, padrao)));
        }

        return query;
    }
}

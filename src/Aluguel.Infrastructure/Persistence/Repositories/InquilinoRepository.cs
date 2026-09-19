using Aluguel.Application.Abstractions;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using Aluguel.Domain.Contratos;
using Aluguel.Domain.Inquilinos;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

/// <summary>
/// As consultas usam <c>db.Inquilinos</c>, que já aplica os filtros globais de tenant e soft delete —
/// garantindo o isolamento multi-tenant automaticamente.
/// </summary>
public class InquilinoRepository(AppDbContext db) : IInquilinoRepository
{
    public Task<Inquilino?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Inquilinos.FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<IReadOnlyList<Inquilino>> ListarAsync(Guid? clienteId, string? termo, TipoPessoa? tipoPessoa,
        StatusAtivoInativo? status, Guid? imovelId, int skip, int take, CancellationToken ct = default) =>
        await Filtrar(clienteId, termo, tipoPessoa, status, imovelId)
            .AsNoTracking()
            .OrderByDescending(i => i.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> ContarAsync(Guid? clienteId, string? termo, TipoPessoa? tipoPessoa,
        StatusAtivoInativo? status, Guid? imovelId, CancellationToken ct = default) =>
        Filtrar(clienteId, termo, tipoPessoa, status, imovelId).CountAsync(ct);

    public Task<bool> PossuiContratoAtivoAsync(Guid inquilinoId, CancellationToken ct = default) =>
        db.Contratos.AnyAsync(c => c.InquilinoId == inquilinoId && c.Status == StatusContrato.Ativo, ct);

    public Task<bool> ImovelPossuiInquilinoAtivoAsync(Guid imovelId, Guid? ignorarInquilinoId = null, CancellationToken ct = default) =>
        db.Inquilinos.AnyAsync(
            i => i.ImovelId == imovelId
                 && i.Status == StatusAtivoInativo.Ativo
                 && (ignorarInquilinoId == null || i.Id != ignorarInquilinoId),
            ct);

    public void Adicionar(Inquilino inquilino) => db.Inquilinos.Add(inquilino);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private IQueryable<Inquilino> Filtrar(
        Guid? clienteId, string? termo, TipoPessoa? tipoPessoa, StatusAtivoInativo? status, Guid? imovelId)
    {
        var query = db.Inquilinos.AsQueryable();

        if (clienteId is { } cid)
            query = query.Where(i => i.ClienteId == cid);

        if (imovelId is { } iid)
            query = query.Where(i => i.ImovelId == iid);

        if (tipoPessoa is { } tp)
            query = query.Where(i => i.TipoPessoa == tp);

        if (status is { } s)
            query = query.Where(i => i.Status == s);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            var digitos = new string(t.Where(char.IsDigit).ToArray());
            var padrao = $"%{t}%";
            var padraoDigitos = $"%{digitos}%";

            query = query.Where(i =>
                EF.Functions.ILike(i.Nome, padrao) ||
                (digitos.Length > 0 && EF.Functions.ILike(i.Documento, padraoDigitos)));
        }

        return query;
    }
}

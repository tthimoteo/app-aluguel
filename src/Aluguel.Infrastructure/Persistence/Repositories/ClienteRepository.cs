using Aluguel.Application.Abstractions;
using Aluguel.Domain.Clientes;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

/// <summary>
/// As consultas usam <c>db.Clientes</c>, que já aplica os filtros globais de tenant e soft delete
/// do <see cref="AppDbContext"/> — garantindo o isolamento multi-tenant automaticamente.
/// </summary>
public class ClienteRepository(AppDbContext db) : IClienteRepository
{
    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        db.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Cliente>> ListarAsync(string? termo, TipoPessoa? tipoPessoa,
        int skip, int take, CancellationToken ct = default) =>
        await Filtrar(termo, tipoPessoa)
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> ContarAsync(string? termo, TipoPessoa? tipoPessoa, CancellationToken ct = default) =>
        Filtrar(termo, tipoPessoa).CountAsync(ct);

    public Task<bool> CpfEmUsoAsync(string cpf, Guid? ignorarId, CancellationToken ct = default) =>
        db.Clientes.AnyAsync(c => c.Cpf == cpf && (ignorarId == null || c.Id != ignorarId), ct);

    public Task<bool> CnpjEmUsoAsync(string cnpj, Guid? ignorarId, CancellationToken ct = default) =>
        db.Clientes.AnyAsync(c => c.Cnpj == cnpj && (ignorarId == null || c.Id != ignorarId), ct);

    public void Adicionar(Cliente cliente) => db.Clientes.Add(cliente);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);

    private IQueryable<Cliente> Filtrar(string? termo, TipoPessoa? tipoPessoa)
    {
        var query = db.Clientes.AsQueryable();

        if (tipoPessoa is { } tp)
            query = query.Where(c => c.TipoPessoa == tp);

        if (!string.IsNullOrWhiteSpace(termo))
        {
            var t = termo.Trim();
            var digitos = new string(t.Where(char.IsDigit).ToArray());
            var padrao = $"%{t}%";
            var padraoDigitos = $"%{digitos}%";

            query = query.Where(c =>
                (c.Nome != null && EF.Functions.ILike(c.Nome, padrao)) ||
                (c.RazaoSocial != null && EF.Functions.ILike(c.RazaoSocial, padrao)) ||
                (c.NomeFantasia != null && EF.Functions.ILike(c.NomeFantasia, padrao)) ||
                (digitos.Length > 0 && c.Cpf != null && EF.Functions.ILike(c.Cpf, padraoDigitos)) ||
                (digitos.Length > 0 && c.Cnpj != null && EF.Functions.ILike(c.Cnpj, padraoDigitos)));
        }

        return query;
    }
}

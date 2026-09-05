using Aluguel.Application.Abstractions;
using Aluguel.Domain.Clientes;

namespace Aluguel.Application.UnitTests.Fakes;

/// <summary>Repositório em memória para testes de handlers/validadores (sem banco).</summary>
public sealed class FakeClienteRepository : IClienteRepository
{
    public List<Cliente> Itens { get; } = [];
    public int SalvouVezes { get; private set; }

    public Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Itens.FirstOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<Cliente>> ListarAsync(string? termo, TipoPessoa? tipoPessoa,
        int skip, int take, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Cliente>>(Itens.Skip(skip).Take(take).ToList());

    public Task<int> ContarAsync(string? termo, TipoPessoa? tipoPessoa, CancellationToken ct = default) =>
        Task.FromResult(Itens.Count);

    public Task<bool> CpfEmUsoAsync(string cpf, Guid? ignorarId, CancellationToken ct = default) =>
        Task.FromResult(Itens.Any(c => c.Cpf == cpf && c.Id != ignorarId));

    public Task<bool> CnpjEmUsoAsync(string cnpj, Guid? ignorarId, CancellationToken ct = default) =>
        Task.FromResult(Itens.Any(c => c.Cnpj == cnpj && c.Id != ignorarId));

    public void Adicionar(Cliente cliente) => Itens.Add(cliente);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default)
    {
        SalvouVezes++;
        return Task.FromResult(1);
    }
}

public sealed class FakeCurrentTenant(Guid? tenantId) : ICurrentTenant
{
    public Guid? TenantId { get; } = tenantId;
}

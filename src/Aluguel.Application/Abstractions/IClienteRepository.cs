using Aluguel.Domain.Clientes;

namespace Aluguel.Application.Abstractions;

/// <summary>Porta de acesso ao agregado Cliente (implementada na Infrastructure, já escopada por tenant).</summary>
public interface IClienteRepository
{
    Task<Cliente?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Cliente>> ListarAsync(string? termo, TipoPessoa? tipoPessoa,
        int skip, int take, CancellationToken ct = default);

    Task<int> ContarAsync(string? termo, TipoPessoa? tipoPessoa, CancellationToken ct = default);

    Task<bool> CpfEmUsoAsync(string cpf, Guid? ignorarId, CancellationToken ct = default);

    Task<bool> CnpjEmUsoAsync(string cnpj, Guid? ignorarId, CancellationToken ct = default);

    void Adicionar(Cliente cliente);

    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
}

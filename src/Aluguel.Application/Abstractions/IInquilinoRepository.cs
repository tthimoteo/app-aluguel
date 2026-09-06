using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using Aluguel.Domain.Inquilinos;

namespace Aluguel.Application.Abstractions;

/// <summary>Porta de acesso ao agregado Inquilino (implementada na Infrastructure, já escopada por tenant).</summary>
public interface IInquilinoRepository
{
    Task<Inquilino?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Inquilino>> ListarAsync(Guid? clienteId, string? termo, TipoPessoa? tipoPessoa,
        StatusAtivoInativo? status, int skip, int take, CancellationToken ct = default);

    Task<int> ContarAsync(Guid? clienteId, string? termo, TipoPessoa? tipoPessoa,
        StatusAtivoInativo? status, CancellationToken ct = default);

    /// <summary>Dependência para exclusão (§4): existe contrato ativo vinculado ao inquilino?</summary>
    Task<bool> PossuiContratoAtivoAsync(Guid inquilinoId, CancellationToken ct = default);

    void Adicionar(Inquilino inquilino);

    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
}

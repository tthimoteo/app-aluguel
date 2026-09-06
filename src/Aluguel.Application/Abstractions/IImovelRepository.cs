using Aluguel.Domain.Common;
using Aluguel.Domain.Imoveis;

namespace Aluguel.Application.Abstractions;

/// <summary>Porta de acesso ao agregado Imóvel (implementada na Infrastructure, já escopada por tenant).</summary>
public interface IImovelRepository
{
    Task<Imovel?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Imovel>> ListarAsync(Guid? clienteId, string? termo, TipoImovel? tipo,
        StatusAtivoInativo? status, int skip, int take, CancellationToken ct = default);

    Task<int> ContarAsync(Guid? clienteId, string? termo, TipoImovel? tipo,
        StatusAtivoInativo? status, CancellationToken ct = default);

    /// <summary>Regra de plano (UC003): considera apenas os imóveis ativos do cliente.</summary>
    Task<bool> PodeAdicionarImovelAsync(Guid clienteId, CancellationToken ct = default);

    /// <summary>Dependência para exclusão (§4): existe contrato ativo vinculado ao imóvel?</summary>
    Task<bool> PossuiContratoAtivoAsync(Guid imovelId, CancellationToken ct = default);

    void Adicionar(Imovel imovel);

    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
}

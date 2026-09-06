using Aluguel.Domain.Contratos;

namespace Aluguel.Application.Abstractions;

/// <summary>Porta de acesso ao agregado Contrato (implementada na Infrastructure, já escopada por tenant).</summary>
public interface IContratoRepository
{
    Task<Contrato?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Contrato>> ListarAsync(Guid? clienteId, Guid? imovelId, Guid? inquilinoId,
        StatusContrato? status, int skip, int take, CancellationToken ct = default);

    Task<int> ContarAsync(Guid? clienteId, Guid? imovelId, Guid? inquilinoId,
        StatusContrato? status, CancellationToken ct = default);

    /// <summary>CASO 3: indica se o imóvel já possui um contrato ativo (opcionalmente ignorando um id).</summary>
    Task<bool> ImovelPossuiContratoAtivoAsync(Guid imovelId, Guid? ignorarId, CancellationToken ct = default);

    void Adicionar(Contrato contrato);

    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
}

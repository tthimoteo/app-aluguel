using Aluguel.Domain.Assinaturas;

namespace Aluguel.Application.Abstractions;

/// <summary>Porta de acesso ao catálogo de planos (implementada na Infrastructure).</summary>
public interface IPlanoRepository
{
    Task<Plano?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Plano>> ListarAtivosAsync(CancellationToken ct = default);
}

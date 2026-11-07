using Aluguel.Application.Relatorios;

namespace Aluguel.Application.Abstractions;

public interface IRelatorioRepository
{
    Task<RelatorioPainelDto> ObterPainelAsync(
        Guid? clienteId,
        Guid? imovelId,
        int? ano,
        int? mes,
        DateOnly? dataInicio,
        DateOnly? dataFim,
        int? diasAtrasoMinimos,
        DateOnly hoje,
        CancellationToken ct = default);
}

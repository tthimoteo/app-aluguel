using Aluguel.Application.Abstractions;
using MediatR;

namespace Aluguel.Application.Planos.ObterPlanos;

/// <summary>Query CQRS para listar os planos ativos do catálogo.</summary>
public sealed record ObterPlanosQuery : IRequest<IReadOnlyList<PlanoDto>>;

public sealed class ObterPlanosQueryHandler(IPlanoRepository repositorio)
    : IRequestHandler<ObterPlanosQuery, IReadOnlyList<PlanoDto>>
{
    public async Task<IReadOnlyList<PlanoDto>> Handle(ObterPlanosQuery request, CancellationToken cancellationToken)
    {
        var planos = await repositorio.ListarAtivosAsync(cancellationToken);
        return planos
            .Select(p => new PlanoDto(p.Id, p.Codigo, p.Nome, p.MaxImoveis, p.MaxUsuarios,
                p.PermiteNfse, p.TrialDias, p.ValorMensal))
            .ToList();
    }
}

using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Imoveis.ObterImovelPorId;

public sealed record ObterImovelPorIdQuery(Guid Id) : IRequest<ImovelDto?>;

public sealed class ObterImovelPorIdQueryHandler(IImovelRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<ObterImovelPorIdQuery, ImovelDto?>
{
    public async Task<ImovelDto?> Handle(ObterImovelPorIdQuery request, CancellationToken cancellationToken)
    {
        var imovel = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        return imovel is not null && EscopoCliente.PodeAcessar(currentUser, imovel.ClienteId)
            ? imovel.ParaDto()
            : null;
    }
}

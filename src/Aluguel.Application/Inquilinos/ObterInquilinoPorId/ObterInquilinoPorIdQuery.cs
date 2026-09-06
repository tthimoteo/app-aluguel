using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Inquilinos.ObterInquilinoPorId;

public sealed record ObterInquilinoPorIdQuery(Guid Id) : IRequest<InquilinoDto?>;

public sealed class ObterInquilinoPorIdQueryHandler(IInquilinoRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<ObterInquilinoPorIdQuery, InquilinoDto?>
{
    public async Task<InquilinoDto?> Handle(ObterInquilinoPorIdQuery request, CancellationToken cancellationToken)
    {
        var inquilino = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        return inquilino is not null && EscopoCliente.PodeAcessar(currentUser, inquilino.ClienteId)
            ? inquilino.ParaDto()
            : null;
    }
}

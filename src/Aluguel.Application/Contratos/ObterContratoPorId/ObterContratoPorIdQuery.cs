using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Contratos.ObterContratoPorId;

public sealed record ObterContratoPorIdQuery(Guid Id) : IRequest<ContratoDto?>;

public sealed class ObterContratoPorIdQueryHandler(IContratoRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<ObterContratoPorIdQuery, ContratoDto?>
{
    public async Task<ContratoDto?> Handle(ObterContratoPorIdQuery request, CancellationToken cancellationToken)
    {
        var contrato = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        return contrato is not null && EscopoCliente.PodeAcessar(currentUser, contrato.ClienteId)
            ? contrato.ParaDto()
            : null;
    }
}

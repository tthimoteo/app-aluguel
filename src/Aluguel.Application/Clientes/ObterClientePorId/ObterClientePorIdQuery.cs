using Aluguel.Application.Abstractions;
using MediatR;

namespace Aluguel.Application.Clientes.ObterClientePorId;

public sealed record ObterClientePorIdQuery(Guid Id) : IRequest<ClienteDto?>;

public sealed class ObterClientePorIdQueryHandler(IClienteRepository repositorio)
    : IRequestHandler<ObterClientePorIdQuery, ClienteDto?>
{
    public async Task<ClienteDto?> Handle(ObterClientePorIdQuery request, CancellationToken cancellationToken)
    {
        var cliente = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        return cliente?.ParaDto();
    }
}

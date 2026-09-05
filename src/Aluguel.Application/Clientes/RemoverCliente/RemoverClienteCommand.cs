using Aluguel.Application.Abstractions;
using MediatR;

namespace Aluguel.Application.Clientes.RemoverCliente;

/// <summary>Exclusão lógica do cliente. Retorna false quando o cliente não existe no tenant.</summary>
public sealed record RemoverClienteCommand(Guid Id) : IRequest<bool>;

public sealed class RemoverClienteCommandHandler(IClienteRepository repositorio)
    : IRequestHandler<RemoverClienteCommand, bool>
{
    public async Task<bool> Handle(RemoverClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (cliente is null)
            return false;

        cliente.Remover();
        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        return true;
    }
}

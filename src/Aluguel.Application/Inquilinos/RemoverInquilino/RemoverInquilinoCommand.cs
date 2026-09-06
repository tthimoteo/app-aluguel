using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Inquilinos.RemoverInquilino;

/// <summary>Exclusão lógica do inquilino. Bloqueada quando há contrato ativo vinculado (§4).</summary>
public sealed record RemoverInquilinoCommand(Guid Id) : IRequest<bool>;

public sealed class RemoverInquilinoCommandHandler(IInquilinoRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<RemoverInquilinoCommand, bool>
{
    public async Task<bool> Handle(RemoverInquilinoCommand request, CancellationToken cancellationToken)
    {
        var inquilino = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (inquilino is null || !EscopoCliente.PodeAcessar(currentUser, inquilino.ClienteId))
            return false;

        if (await repositorio.PossuiContratoAtivoAsync(inquilino.Id, cancellationToken))
            throw new InvalidOperationException("Não é possível remover um inquilino com contrato ativo.");

        inquilino.Remover();
        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        return true;
    }
}

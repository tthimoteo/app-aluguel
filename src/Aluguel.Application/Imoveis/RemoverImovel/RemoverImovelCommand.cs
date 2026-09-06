using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Imoveis.RemoverImovel;

/// <summary>Exclusão lógica do imóvel. Bloqueada quando há contrato ativo vinculado (§4).</summary>
public sealed record RemoverImovelCommand(Guid Id) : IRequest<bool>;

public sealed class RemoverImovelCommandHandler(IImovelRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<RemoverImovelCommand, bool>
{
    public async Task<bool> Handle(RemoverImovelCommand request, CancellationToken cancellationToken)
    {
        var imovel = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (imovel is null || !EscopoCliente.PodeAcessar(currentUser, imovel.ClienteId))
            return false;

        if (await repositorio.PossuiContratoAtivoAsync(imovel.Id, cancellationToken))
            throw new InvalidOperationException("Não é possível remover um imóvel com contrato ativo.");

        imovel.Remover();
        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        return true;
    }
}

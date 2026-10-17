using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Cobrancas.RemoverCobranca;

public sealed record RemoverCobrancaCommand(Guid Id) : IRequest<bool>;

public sealed class RemoverCobrancaCommandHandler(
    ICobrancaRepository cobrancas,
    IClienteRepository clientes,
    IPlanoRepository planos,
    ICurrentUser currentUser)
    : IRequestHandler<RemoverCobrancaCommand, bool>
{
    public async Task<bool> Handle(RemoverCobrancaCommand request, CancellationToken cancellationToken)
    {
        var nfse = await cobrancas.ObterNfseAsync(request.Id, cancellationToken);
        if (nfse is null)
            return false;

        if (!EscopoCliente.PodeAcessar(currentUser, nfse.ClienteId))
            throw new InvalidOperationException("Sem permissão para remover esta cobrança.");

        await AcessoCobranca.GarantirPlanoPermiteAsync(
            currentUser, clientes, planos, nfse.ClienteId, cancellationToken);

        if (await cobrancas.ObterPagamentoPorNfseAsync(nfse.Id, cancellationToken) is not null)
            throw new InvalidOperationException(
                "Cobrança já paga não pode ser removida. Somente cobranças pendentes de pagamento podem ser excluídas.");

        await cobrancas.RemoverCobrancaAsync(nfse.Id, cancellationToken);
        await cobrancas.SalvarAlteracoesAsync(cancellationToken);
        return true;
    }
}

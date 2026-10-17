using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Cobrancas.ObterCobranca;

public sealed record ObterCobrancaQuery(Guid Id) : IRequest<CobrancaDetalheDto?>;

public sealed class ObterCobrancaQueryHandler(
    ICobrancaRepository cobrancas,
    IClienteRepository clientes,
    IPlanoRepository planos,
    ICurrentUser currentUser)
    : IRequestHandler<ObterCobrancaQuery, CobrancaDetalheDto?>
{
    public async Task<CobrancaDetalheDto?> Handle(ObterCobrancaQuery request, CancellationToken cancellationToken)
    {
        var detalhe = await cobrancas.ObterDetalheAsync(request.Id, cancellationToken);
        if (detalhe is null || !EscopoCliente.PodeAcessar(currentUser, detalhe.ClienteId))
            return null;

        await AcessoCobranca.GarantirPlanoPermiteAsync(
            currentUser, clientes, planos, detalhe.ClienteId, cancellationToken);

        return detalhe;
    }
}

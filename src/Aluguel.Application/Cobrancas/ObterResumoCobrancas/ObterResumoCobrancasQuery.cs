using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Cobrancas.ObterResumoCobrancas;

public sealed record ObterResumoCobrancasQuery(Guid? ClienteId = null) : IRequest<CobrancaResumoDto>;

public sealed class ObterResumoCobrancasQueryHandler(
    ICobrancaRepository cobrancas,
    IClienteRepository clientes,
    IPlanoRepository planos,
    ICurrentUser currentUser)
    : IRequestHandler<ObterResumoCobrancasQuery, CobrancaResumoDto>
{
    public async Task<CobrancaResumoDto> Handle(
        ObterResumoCobrancasQuery request, CancellationToken cancellationToken)
    {
        var clienteEscopo = EscopoCliente.Resolver(currentUser, request.ClienteId);
        if (!currentUser.EhAdministrador && clienteEscopo is null)
            return new CobrancaResumoDto(0, 0);

        if (clienteEscopo is { } cid)
        {
            if (!EscopoCliente.PodeAcessar(currentUser, cid))
                return new CobrancaResumoDto(0, 0);

            await AcessoCobranca.GarantirPlanoPermiteAsync(
                currentUser, clientes, planos, cid, cancellationToken);
        }

        var hoje = HojeBrasilia();
        return await cobrancas.ContarResumoVencimentosAsync(clienteEscopo, hoje, cancellationToken);
    }

    private static DateOnly HojeBrasilia()
    {
        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        }
        catch (TimeZoneNotFoundException)
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time");
        }

        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz));
    }
}

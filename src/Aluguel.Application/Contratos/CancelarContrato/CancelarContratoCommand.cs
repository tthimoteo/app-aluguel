using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Contratos.CancelarContrato;

/// <summary>Cancela o contrato, liberando o imóvel para novo contrato (CASO 3).</summary>
public sealed record CancelarContratoCommand(Guid Id) : IRequest<ContratoDto?>;

public sealed class CancelarContratoCommandHandler(IContratoRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<CancelarContratoCommand, ContratoDto?>
{
    public async Task<ContratoDto?> Handle(CancelarContratoCommand request, CancellationToken cancellationToken)
    {
        var contrato = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (contrato is null || !EscopoCliente.PodeAcessar(currentUser, contrato.ClienteId))
            return null;

        contrato.Cancelar();
        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        return contrato.ParaDto();
    }
}

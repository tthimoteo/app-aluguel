using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Contratos.EncerrarContrato;

/// <summary>Encerra o contrato (fim natural), liberando o imóvel para novo contrato (CASO 3).</summary>
public sealed record EncerrarContratoCommand(Guid Id) : IRequest<ContratoDto?>;

public sealed class EncerrarContratoCommandHandler(IContratoRepository repositorio, ICurrentUser currentUser)
    : IRequestHandler<EncerrarContratoCommand, ContratoDto?>
{
    public async Task<ContratoDto?> Handle(EncerrarContratoCommand request, CancellationToken cancellationToken)
    {
        var contrato = await repositorio.ObterPorIdAsync(request.Id, cancellationToken);
        if (contrato is null || !EscopoCliente.PodeAcessar(currentUser, contrato.ClienteId))
            return null;

        contrato.Encerrar();
        await repositorio.SalvarAlteracoesAsync(cancellationToken);
        return contrato.ParaDto();
    }
}

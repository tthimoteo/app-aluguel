using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Assinaturas;
using MediatR;

namespace Aluguel.Application.Assinaturas.ObterAssinaturaAtual;

public sealed record ObterAssinaturaAtualQuery : IRequest<AssinaturaAtualDto?>;

public sealed class ObterAssinaturaAtualQueryHandler(
    IAssinaturaRepository assinaturas,
    IPlanoRepository planos,
    ICurrentUser currentUser)
    : IRequestHandler<ObterAssinaturaAtualQuery, AssinaturaAtualDto?>
{
    public async Task<AssinaturaAtualDto?> Handle(
        ObterAssinaturaAtualQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.ClienteId is not { } clienteId)
            return null;

        if (!EscopoCliente.PodeAcessar(currentUser, clienteId))
            return null;

        var assinatura = await assinaturas.ObterVigentePorClienteAsync(clienteId, cancellationToken);
        if (assinatura is null)
            return null;

        var plano = await planos.ObterPorIdAsync(assinatura.PlanoId, cancellationToken);
        Plano? agendado = null;
        if (assinatura.PlanoAgendadoId is { } aid)
            agendado = await planos.ObterPorIdAsync(aid, cancellationToken);

        return new AssinaturaAtualDto(
            assinatura.Id,
            assinatura.ClienteId,
            assinatura.PlanoId,
            plano?.Nome ?? "Plano",
            plano?.Codigo ?? "",
            assinatura.Status.ToString(),
            assinatura.ValorMensal,
            assinatura.ProximaCobranca,
            assinatura.FimCicloPago,
            assinatura.PlanoAgendadoId,
            agendado?.Nome,
            assinatura.CancelamentoSolicitadoEm,
            assinatura.CancelamentoPendente,
            assinatura.DowngradeAgendado);
    }
}

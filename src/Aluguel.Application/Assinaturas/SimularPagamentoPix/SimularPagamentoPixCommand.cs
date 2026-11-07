using System.Text.Json;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Assinaturas.ProcessarWebhookPixInter;
using Aluguel.Application.Common.Acesso;
using MediatR;

namespace Aluguel.Application.Assinaturas.SimularPagamentoPix;

/// <summary>
/// Somente desenvolvimento/Mock: simula o webhook de liquidação do Banco Inter para um pagamento.
/// </summary>
public sealed record SimularPagamentoPixCommand(Guid PagamentoId) : IRequest<ProcessarWebhookPixInterResultado>;

public sealed class SimularPagamentoPixCommandHandler(
    IAssinaturaRepository assinaturas,
    ICurrentUser currentUser,
    ISender sender)
    : IRequestHandler<SimularPagamentoPixCommand, ProcessarWebhookPixInterResultado>
{
    public async Task<ProcessarWebhookPixInterResultado> Handle(
        SimularPagamentoPixCommand request, CancellationToken cancellationToken)
    {
        var assinatura = await assinaturas.ObterPorPagamentoIdAsync(request.PagamentoId, cancellationToken)
            ?? throw new InvalidOperationException("Pagamento não encontrado.");

        if (!EscopoCliente.PodeAcessar(currentUser, assinatura.ClienteId))
            throw new InvalidOperationException("Sem permissão para simular este pagamento.");

        var pagamento = assinatura.Pagamentos.FirstOrDefault(p => p.Id == request.PagamentoId)
            ?? await assinaturas.ObterPagamentoPorIdAsync(request.PagamentoId, cancellationToken)
            ?? throw new InvalidOperationException("Pagamento não encontrado.");

        if (string.IsNullOrWhiteSpace(pagamento.PixTxId))
            throw new InvalidOperationException("Pagamento sem txid PIX.");

        var payload = JsonSerializer.Serialize(new
        {
            pix = new[]
            {
                new
                {
                    endToEndId = $"EDEV{Guid.NewGuid():N}"[..32],
                    txid = pagamento.PixTxId,
                    valor = pagamento.Valor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                    horario = DateTimeOffset.UtcNow.ToString("O"),
                },
            },
        });

        return await sender.Send(new ProcessarWebhookPixInterCommand(payload), cancellationToken);
    }
}

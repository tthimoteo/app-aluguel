using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Assinaturas;
using MediatR;

namespace Aluguel.Application.Assinaturas.ObterPagamentoPlano;

public sealed record ObterPagamentoPlanoQuery(Guid PagamentoId) : IRequest<PagamentoPlanoDto?>;

public sealed class ObterPagamentoPlanoQueryHandler(
    IAssinaturaRepository assinaturas,
    IPlanoRepository planos,
    IInterPixProvider pix,
    ICurrentUser currentUser)
    : IRequestHandler<ObterPagamentoPlanoQuery, PagamentoPlanoDto?>
{
    public async Task<PagamentoPlanoDto?> Handle(
        ObterPagamentoPlanoQuery request, CancellationToken cancellationToken)
    {
        var assinatura = await assinaturas.ObterPorPagamentoIdAsync(request.PagamentoId, cancellationToken);
        if (assinatura is null)
            return null;

        if (!EscopoCliente.PodeAcessar(currentUser, assinatura.ClienteId))
            return null;

        var pagamento = assinatura.Pagamentos.FirstOrDefault(p => p.Id == request.PagamentoId)
            ?? await assinaturas.ObterPagamentoPorIdAsync(request.PagamentoId, cancellationToken);
        if (pagamento is null)
            return null;

        var plano = await planos.ObterPorIdAsync(pagamento.PlanoDestinoId, cancellationToken);
        var beneficiario = pix.ObterBeneficiario();

        return new PagamentoPlanoDto(
            pagamento.Id,
            assinatura.Id,
            assinatura.ClienteId,
            pagamento.PlanoDestinoId,
            plano?.Nome ?? "Plano",
            pagamento.Valor,
            pagamento.Status.ToString(),
            pagamento.MetodoPagamento?.ToString(),
            pagamento.DataVencimento,
            pagamento.DataPagamento,
            pagamento.PixTxId,
            pagamento.PixCopiaCola,
            pagamento.PixQrCodeBase64,
            new BeneficiarioPixDto(
                beneficiario.Nome, beneficiario.Cnpj, beneficiario.Banco, beneficiario.ContaCorrente));
    }
}

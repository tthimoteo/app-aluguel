using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Financeiro;
using MediatR;

namespace Aluguel.Application.Cobrancas.RegistrarPagamentoCobranca;

public sealed record RegistrarPagamentoCobrancaCommand(
    Guid Id,
    DateOnly? DataPagamento = null,
    decimal? ValorPago = null) : IRequest<CobrancaDetalheDto>;

public sealed class RegistrarPagamentoCobrancaCommandHandler(
    ICobrancaRepository cobrancas,
    IClienteRepository clientes,
    IPlanoRepository planos,
    ICurrentUser currentUser)
    : IRequestHandler<RegistrarPagamentoCobrancaCommand, CobrancaDetalheDto>
{
    public async Task<CobrancaDetalheDto> Handle(
        RegistrarPagamentoCobrancaCommand request, CancellationToken cancellationToken)
    {
        var nfse = await cobrancas.ObterNfseAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException("Cobrança não encontrada.");

        if (!EscopoCliente.PodeAcessar(currentUser, nfse.ClienteId))
            throw new InvalidOperationException("Sem permissão para marcar pagamento desta cobrança.");

        await AcessoCobranca.GarantirPlanoPermiteAsync(
            currentUser, clientes, planos, nfse.ClienteId, cancellationToken);

        if (await cobrancas.ObterPagamentoPorNfseAsync(nfse.Id, cancellationToken) is not null)
            throw new InvalidOperationException("Esta cobrança já está marcada como paga.");

        var valor = request.ValorPago ?? nfse.ValorFaturado;
        if (valor <= 0)
            throw new ArgumentException("Valor pago deve ser maior que zero.");

        var data = request.DataPagamento ?? DateOnly.FromDateTime(DateTime.UtcNow);

        cobrancas.AdicionarPagamento(new Pagamento(
            nfse.TenantId,
            nfse.ClienteId,
            nfse.ImovelId,
            nfse.Competencia,
            valor,
            data,
            contratoId: nfse.ContratoId,
            nfseId: nfse.Id));

        var boleto = await cobrancas.ObterBoletoPorNfseAsync(nfse.Id, cancellationToken);
        boleto?.MarcarPago();

        await cobrancas.SalvarAlteracoesAsync(cancellationToken);
        return (await cobrancas.ObterDetalheAsync(nfse.Id, cancellationToken))!;
    }
}

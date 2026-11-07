using Aluguel.Application.Abstractions;
using Aluguel.Domain.Assinaturas;

namespace Aluguel.Application.Assinaturas;

internal static class AssinaturaPixHelper
{
    /// <summary>txid Inter: 26–35 caracteres [a-zA-Z0-9].</summary>
    public static string GerarTxId()
    {
        var raw = $"lucrare{Guid.NewGuid():N}";
        return raw.Length > 35 ? raw[..35] : raw.PadRight(26, '0');
    }

    public static PagamentoPlanoDto ParaDto(
        PagamentoPlano pagamento,
        Guid clienteId,
        string planoNome,
        InterPixBeneficiario beneficiario) =>
        new(
            pagamento.Id,
            pagamento.AssinaturaId,
            clienteId,
            pagamento.PlanoDestinoId,
            planoNome,
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

    public static async Task<(PagamentoPlano Pagamento, string CopiaCola)> CriarCobrancaPixAsync(
        Assinatura assinatura,
        Guid planoDestinoId,
        decimal valor,
        string descricao,
        bool assinaturaJaRastreada,
        IAssinaturaRepository assinaturas,
        IInterPixProvider pix,
        DateTimeOffset agora,
        CancellationToken ct)
    {
        var txId = GerarTxId();
        var pagamento = assinatura.CriarPagamentoPix(planoDestinoId, valor, agora.AddHours(24), txId);
        if (assinaturaJaRastreada)
            assinaturas.AdicionarPagamento(pagamento);

        var cob = await pix.CriarCobrancaImediataAsync(
            new InterPixCobrancaPedido(TxId: txId, Valor: valor, SolicitacaoPagador: descricao), ct);

        if (!cob.Sucesso || string.IsNullOrWhiteSpace(cob.PixCopiaCola))
        {
            pagamento.Cancelar(cob.MensagemErro ?? "Falha ao criar cobrança PIX no Banco Inter");
            await assinaturas.SalvarAlteracoesAsync(ct);
            throw new InvalidOperationException(
                cob.MensagemErro ?? "Não foi possível gerar o QR Code PIX. Tente novamente em instantes.");
        }

        pagamento.RegistrarQrCode(cob.PixCopiaCola, cob.QrCodeBase64, cob.Location);
        return (pagamento, cob.PixCopiaCola);
    }
}

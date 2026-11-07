namespace Aluguel.Application.Abstractions;

/// <summary>
/// Porta da API PIX do Banco Inter (cobrança imediata + webhook de liquidação).
/// Credenciais da plataforma (conta TTHIMOTEO), distintas da integração de boleto por cliente.
/// </summary>
public interface IInterPixProvider
{
    Task<InterPixCobrancaResultado> CriarCobrancaImediataAsync(
        InterPixCobrancaPedido pedido, CancellationToken ct = default);

    /// <summary>Dados do beneficiário exibidos na tela de pagamento.</summary>
    InterPixBeneficiario ObterBeneficiario();
}

public sealed record InterPixCobrancaPedido(
    string TxId,
    decimal Valor,
    string SolicitacaoPagador,
    int ExpiracaoSegundos = 3600);

public sealed record InterPixCobrancaResultado(
    bool Sucesso,
    string? TxId = null,
    string? PixCopiaCola = null,
    string? QrCodeBase64 = null,
    string? Location = null,
    string? Status = null,
    string? MensagemErro = null);

public sealed record InterPixBeneficiario(
    string Nome,
    string Cnpj,
    string Banco,
    string ContaCorrente);

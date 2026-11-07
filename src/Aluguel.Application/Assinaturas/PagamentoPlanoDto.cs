namespace Aluguel.Application.Assinaturas;

public sealed record PagamentoPlanoDto(
    Guid Id,
    Guid AssinaturaId,
    Guid ClienteId,
    Guid PlanoDestinoId,
    string PlanoNome,
    decimal Valor,
    string Status,
    string? MetodoPagamento,
    DateTimeOffset DataVencimento,
    DateTimeOffset? DataPagamento,
    string? PixTxId,
    string? PixCopiaCola,
    string? PixQrCodeBase64,
    BeneficiarioPixDto Beneficiario);

public sealed record BeneficiarioPixDto(
    string Nome,
    string Cnpj,
    string Banco,
    string ContaCorrente);

public sealed record ContratarPlanoResultadoDto(
    Guid AssinaturaId,
    Guid PagamentoId,
    string StatusAssinatura,
    PagamentoPlanoDto Pagamento);

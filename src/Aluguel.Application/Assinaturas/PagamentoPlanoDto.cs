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
    Guid? PagamentoId,
    string StatusAssinatura,
    /// <summary>Contratacao | Upgrade | DowngradeAgendado.</summary>
    string Operacao,
    string Mensagem,
    PagamentoPlanoDto? Pagamento,
    DateTimeOffset? ProximaCobranca,
    DateTimeOffset? FimCicloPago,
    Guid? PlanoAgendadoId,
    string? PlanoAgendadoNome);

public sealed record AssinaturaAtualDto(
    Guid Id,
    Guid ClienteId,
    Guid PlanoId,
    string PlanoNome,
    string PlanoCodigo,
    string Status,
    decimal ValorMensal,
    DateTimeOffset? ProximaCobranca,
    DateTimeOffset? FimCicloPago,
    Guid? PlanoAgendadoId,
    string? PlanoAgendadoNome,
    DateTimeOffset? CancelamentoSolicitadoEm,
    bool CancelamentoPendente,
    bool DowngradeAgendado);

public sealed record CancelarAssinaturaResultadoDto(
    Guid AssinaturaId,
    string StatusAssinatura,
    DateTimeOffset? FimCicloPago,
    DateTimeOffset CancelamentoSolicitadoEm,
    string Mensagem);

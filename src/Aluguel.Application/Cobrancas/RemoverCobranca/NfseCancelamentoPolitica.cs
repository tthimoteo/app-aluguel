namespace Aluguel.Application.Cobrancas.RemoverCobranca;

/// <summary>
/// Regra de produto: remoção de cobrança cancela a NFS-e automaticamente na SEFIN
/// se a emissão tiver menos de 24h; após esse prazo o cancelamento fiscal é manual no portal.
/// </summary>
public static class NfseCancelamentoPolitica
{
    public static readonly TimeSpan PrazoCancelamentoAutomatico = TimeSpan.FromHours(24);

    /// <summary>
    /// Texto de <c>xMotivo</c> (TSMotivo): 15–255 chars no padrão SEFIN <c>!-ÿ</c>
    /// (Latin-1). Evitar travessão tipográfico e demais Unicode fora de Latin-1.
    /// </summary>
    public const string MotivoRemocaoCobranca =
        "Remocao de cobranca no APP Aluguel - cancelamento solicitado pelo emitente.";

    public const string CodigoMotivoErroEmissao = "1";

    public const string MensagemSemNfseEmitida = "Cobrança removida.";

    public const string MensagemCanceladaAutomaticamente =
        "Cobrança removida e NFS-e cancelada automaticamente na SEFIN (emitida há menos de 24 horas).";

    public const string MensagemRequerCancelamentoManual =
        "A cobrança foi cancelada no sistema. Como a NFS-e foi emitida há mais de 24 horas, " +
        "o cancelamento fiscal deve ser feito manualmente no portal nacional da NFS-e (gov.br).";

    public static bool DentroDoPrazoAutomatico(DateTimeOffset? dataEmissao, DateTimeOffset agoraUtc) =>
        dataEmissao is { } em && agoraUtc - em < PrazoCancelamentoAutomatico;
}

public enum NfseCancelamentoAoRemover
{
    SemNfseEmitida = 0,
    CanceladaAutomaticamente = 1,
    RequerCancelamentoManual = 2,
}

public sealed record RemoverCobrancaResultado(
    bool Removida,
    NfseCancelamentoAoRemover CancelamentoNfse,
    string Mensagem);

namespace Aluguel.Application.Cobrancas;

public sealed record CobrancaListItemDto(
    Guid Id,
    Guid ClienteId,
    string ClienteNome,
    Guid ImovelId,
    string ImovelNome,
    string Competencia,
    decimal ValorFaturado,
    string StatusNfse,
    bool TemPagamento,
    DateOnly? DataPagamento,
    bool TemXml,
    bool TemPdfNfse,
    bool TemBoletoPdf);

public sealed record CobrancaDetalheDto(
    Guid Id,
    Guid ClienteId,
    string ClienteNome,
    Guid ImovelId,
    string ImovelNome,
    Guid? InquilinoId,
    string? InquilinoNome,
    string Competencia,
    long? NumeroNfse,
    string? Serie,
    string? ChaveAcesso,
    string StatusNfse,
    decimal ValorServico,
    decimal Desconto,
    decimal Multa,
    decimal Juros,
    decimal ValorFaturado,
    DateTimeOffset? DataEmissao,
    bool TemPagamento,
    DateOnly? DataPagamento,
    decimal? ValorPago,
    bool TemXml,
    bool TemPdfNfse,
    bool TemBoletoPdf,
    Guid? BoletoId,
    string? NossoNumero,
    string? LinhaDigitavel,
    DateOnly? DataVencimentoBoleto,
    decimal? ValorBoleto,
    string? StatusBoleto);

public enum TipoDownloadCobranca
{
    NfseXml = 1,
    NfsePdf = 2,
    BoletoPdf = 3,
}

public sealed record ArquivoCobrancaDto(
    Stream Conteudo,
    string ContentType,
    string NomeArquivo);

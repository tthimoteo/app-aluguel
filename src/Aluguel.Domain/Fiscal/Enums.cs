namespace Aluguel.Domain.Fiscal;

public enum StatusNfse
{
    Rascunho = 1,
    EmProcessamento = 2,
    Emitida = 3,
    Rejeitada = 4,
    CancelamentoSolicitado = 5,
    Cancelada = 6,
}

public enum TipoDocumentoFiscal
{
    XML = 1,
    PDF = 2,
    XMLCancelamento = 3,
}

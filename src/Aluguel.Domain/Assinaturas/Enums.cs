namespace Aluguel.Domain.Assinaturas;

public enum StatusAssinatura
{
    Trial = 1,
    PendentePagamento = 2,
    Ativa = 3,
    Suspensa = 4,
    Cancelada = 5,
}

public enum StatusPagamentoPlano
{
    Pendente = 1,
    Pago = 2,
    Falhou = 3,
    Estornado = 4,
}

public enum MetodoPagamento
{
    PIX = 1,
    Cartao = 2,
}

public enum EventoAssinatura
{
    InicioTrial = 1,
    Contratacao = 2,
    Renovacao = 3,
    Pagamento = 4,
    FalhaPagamento = 5,
    Suspensao = 6,
    Reativacao = 7,
    Upgrade = 8,
    Downgrade = 9,
    Cancelamento = 10,
}

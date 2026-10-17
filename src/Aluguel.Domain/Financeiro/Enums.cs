namespace Aluguel.Domain.Financeiro;

public enum TipoDespesa
{
    IPTU = 1,
    Outra = 2,
}

public enum StatusBoleto
{
    Pendente = 1,
    Gerado = 2,
    Pago = 3,
    Cancelado = 4,
    Vencido = 5,
}

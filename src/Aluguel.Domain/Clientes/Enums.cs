namespace Aluguel.Domain.Clientes;

public enum TipoPessoa
{
    PF = 1,
    PJ = 2,
}

/// <summary>Situação comercial do cliente (espelha o status da assinatura, §3).</summary>
public enum StatusCliente
{
    Trial = 1,
    PendentePagamento = 2,
    Ativa = 3,
    Suspensa = 4,
    Cancelada = 5,
}

/// <summary>Regime tributário do emitente para NFS-e (Minha Conta / dados fiscais).</summary>
public enum RegimeTributario
{
    SimplesNacional = 1,
    LucroPresumido = 2,
    LucroReal = 3,
    Mei = 4,
}

namespace Aluguel.Domain.Clientes;

public enum TipoPessoa
{
    PF = 1,
    PJ = 2,
}

/// <summary>
/// Situação operacional do cliente na plataforma (gestão pelo Administrador).
/// O ciclo comercial Trial/Ativa/Suspensa permanece em <c>StatusAssinatura</c> (§3).
/// </summary>
public enum StatusCliente
{
    Ativo = 1,
    Inativo = 2,
    Bloqueado = 3,
}

/// <summary>Regime tributário do emitente para NFS-e (Minha Conta / dados fiscais).</summary>
public enum RegimeTributario
{
    SimplesNacional = 1,
    LucroPresumido = 2,
    LucroReal = 3,
    Mei = 4,
}

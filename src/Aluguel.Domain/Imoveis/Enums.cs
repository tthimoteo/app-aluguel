namespace Aluguel.Domain.Imoveis;

public enum TipoImovel
{
    Residencial = 1,
    Comercial = 2,
    Galpao = 3,
    Sala = 4,
    Outro = 9,
}

/// <summary>Propósito da locação para cadastro de cobrança do imóvel (§7 / NFS-e).</summary>
public enum PropositoLocacao
{
    Comercial = 1,
    Residencial = 2,
    AdministracaoDeImoveis = 3,
    IntermediacaoImobiliaria = 4,
}

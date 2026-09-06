using Aluguel.Domain.Common;

namespace Aluguel.Domain.Contratos;

/// <summary>Contrato de aluguel entre imóvel e inquilino (§9). Apenas 1 ativo por imóvel (CASO 3).</summary>
public class Contrato : AggregateRoot, ITenantOwned, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ImovelId { get; private set; }
    public Guid InquilinoId { get; private set; }
    public string NumeroContrato { get; private set; } = default!;
    public StatusContrato Status { get; private set; } = StatusContrato.Ativo;
    public DateOnly DataInicio { get; private set; }
    public DateOnly? DataFimPrevista { get; private set; }
    public int DiaVencimento { get; private set; }
    public decimal ValorAluguel { get; private set; }
    public decimal? JurosAtrasoPct { get; private set; }
    public decimal? MultaAtrasoPct { get; private set; }
    public string? AnexoPath { get; private set; }   // bucket 'contracts'

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Contrato() { }

    public Contrato(Guid tenantId, Guid clienteId, Guid imovelId, Guid inquilinoId, string numeroContrato,
        DateOnly dataInicio, int diaVencimento, decimal valorAluguel, DateOnly? dataFimPrevista = null,
        decimal? jurosAtrasoPct = null, decimal? multaAtrasoPct = null)
    {
        if (string.IsNullOrWhiteSpace(numeroContrato))
            throw new ArgumentException("Número do contrato é obrigatório.", nameof(numeroContrato));
        ValidarRegras(diaVencimento, valorAluguel, dataInicio, dataFimPrevista);

        TenantId = tenantId;
        ClienteId = clienteId;
        ImovelId = imovelId;
        InquilinoId = inquilinoId;
        NumeroContrato = numeroContrato;
        DataInicio = dataInicio;
        DiaVencimento = diaVencimento;
        ValorAluguel = valorAluguel;
        DataFimPrevista = dataFimPrevista;
        JurosAtrasoPct = jurosAtrasoPct;
        MultaAtrasoPct = multaAtrasoPct;
    }

    /// <summary>Atualiza cláusulas do contrato. Permitido apenas enquanto Ativo.</summary>
    public void Atualizar(DateOnly? dataFimPrevista, int diaVencimento, decimal valorAluguel,
        decimal? jurosAtrasoPct, decimal? multaAtrasoPct, string? anexoPath)
    {
        if (Status != StatusContrato.Ativo)
            throw new InvalidOperationException("Somente contratos ativos podem ser alterados.");
        ValidarRegras(diaVencimento, valorAluguel, DataInicio, dataFimPrevista);

        DataFimPrevista = dataFimPrevista;
        DiaVencimento = diaVencimento;
        ValorAluguel = valorAluguel;
        JurosAtrasoPct = jurosAtrasoPct;
        MultaAtrasoPct = multaAtrasoPct;
        AnexoPath = anexoPath;
        Touch();
    }

    /// <summary>Encerra o contrato (fim natural). Libera o imóvel (CASO 3).</summary>
    public void Encerrar()
    {
        if (Status != StatusContrato.Ativo)
            throw new InvalidOperationException("Somente contratos ativos podem ser encerrados.");
        Status = StatusContrato.Encerrado;
        Touch();
    }

    /// <summary>Cancela o contrato. Libera o imóvel (CASO 3).</summary>
    public void Cancelar()
    {
        if (Status != StatusContrato.Ativo)
            throw new InvalidOperationException("Somente contratos ativos podem ser cancelados.");
        Status = StatusContrato.Cancelado;
        Touch();
    }

    private static void ValidarRegras(int diaVencimento, decimal valorAluguel, DateOnly dataInicio, DateOnly? dataFimPrevista)
    {
        if (diaVencimento is < 1 or > 31)
            throw new ArgumentException("Dia de vencimento deve estar entre 1 e 31.", nameof(diaVencimento));
        if (valorAluguel <= 0)
            throw new ArgumentException("Valor do aluguel deve ser positivo.", nameof(valorAluguel));
        if (dataFimPrevista is { } fim && fim < dataInicio)
            throw new ArgumentException("Data fim prevista não pode ser anterior à data de início.", nameof(dataFimPrevista));
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}

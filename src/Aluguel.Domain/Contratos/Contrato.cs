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
}

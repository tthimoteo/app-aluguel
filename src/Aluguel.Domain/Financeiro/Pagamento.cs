using Aluguel.Domain.Common;

namespace Aluguel.Domain.Financeiro;

/// <summary>Pagamento de aluguel de uma competência (§12). Único por imóvel/competência (CASO 6).</summary>
public class Pagamento : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ImovelId { get; private set; }
    public Guid? ContratoId { get; private set; }
    public Guid? NfseId { get; private set; }
    public string Competencia { get; private set; } = default!;   // "MM/AAAA"
    public decimal ValorPago { get; private set; }
    public DateOnly DataPagamento { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Pagamento() { }

    public Pagamento(Guid tenantId, Guid clienteId, Guid imovelId, string competencia, decimal valorPago,
        DateOnly dataPagamento, Guid? contratoId = null, Guid? nfseId = null)
    {
        TenantId = tenantId;
        ClienteId = clienteId;
        ImovelId = imovelId;
        Competencia = competencia;
        ValorPago = valorPago;
        DataPagamento = dataPagamento;
        ContratoId = contratoId;
        NfseId = nfseId;
    }
}

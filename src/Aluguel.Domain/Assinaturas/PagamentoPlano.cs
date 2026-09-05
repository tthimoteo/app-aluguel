using Aluguel.Domain.Common;

namespace Aluguel.Domain.Assinaturas;

/// <summary>Cobrança/pagamento de um ciclo da assinatura (Mercado Pago).</summary>
public class PagamentoPlano : Entity, ITenantOwned, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid AssinaturaId { get; private set; }
    public decimal Valor { get; private set; }
    public DateTimeOffset DataVencimento { get; private set; }
    public DateTimeOffset? DataPagamento { get; private set; }
    public StatusPagamentoPlano Status { get; private set; } = StatusPagamentoPlano.Pendente;
    public MetodoPagamento? MetodoPagamento { get; private set; }
    public string? MercadoPagoPaymentId { get; private set; }
    public string? Observacao { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private PagamentoPlano() { }

    public PagamentoPlano(Guid tenantId, Guid assinaturaId, decimal valor, DateTimeOffset dataVencimento)
    {
        TenantId = tenantId;
        AssinaturaId = assinaturaId;
        Valor = valor;
        DataVencimento = dataVencimento;
    }
}

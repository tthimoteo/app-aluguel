using Aluguel.Domain.Common;

namespace Aluguel.Domain.Assinaturas;

/// <summary>Assinatura do cliente com máquina de estados (§3).</summary>
public class Assinatura : AggregateRoot, ITenantOwned, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid PlanoId { get; private set; }
    public StatusAssinatura Status { get; private set; } = StatusAssinatura.Trial;
    public DateTimeOffset DataInicio { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DataFimTrial { get; private set; }
    public DateTimeOffset? ProximaCobranca { get; private set; }
    public DateTimeOffset? InicioTolerancia { get; private set; }
    public DateTimeOffset? CanceladaEm { get; private set; }
    public DateTimeOffset? FimCicloPago { get; private set; }
    public decimal ValorMensal { get; private set; }
    public string? MercadoPagoSubscriptionId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private readonly List<PagamentoPlano> _pagamentos = new();
    public IReadOnlyCollection<PagamentoPlano> Pagamentos => _pagamentos.AsReadOnly();

    private Assinatura() { }

    public Assinatura(Guid tenantId, Guid clienteId, Guid planoId, decimal valorMensal,
        StatusAssinatura status = StatusAssinatura.Trial)
    {
        TenantId = tenantId;
        ClienteId = clienteId;
        PlanoId = planoId;
        ValorMensal = valorMensal;
        Status = status;
    }
}

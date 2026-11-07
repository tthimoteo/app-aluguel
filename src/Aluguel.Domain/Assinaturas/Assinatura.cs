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
    /// <summary>Legado Mercado Pago — mantido para compatibilidade de schema.</summary>
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
        if (status == StatusAssinatura.Trial)
            DataFimTrial = DateTimeOffset.UtcNow.AddDays(7);
    }

    /// <summary>
    /// Inicia contratação/troca de plano: fica <see cref="StatusAssinatura.PendentePagamento"/>
    /// até liquidação do PIX (webhook Banco Inter).
    /// </summary>
    public void SolicitarContratacao(Guid planoDestinoId, decimal valorMensal)
    {
        if (Status is StatusAssinatura.Cancelada)
            throw new InvalidOperationException("Assinatura cancelada não pode ser recontratada nesta operação.");

        PlanoId = planoDestinoId;
        ValorMensal = valorMensal;
        Status = StatusAssinatura.PendentePagamento;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Ativa a assinatura após confirmação de pagamento PIX.</summary>
    public void AtivarAposPagamento(DateTimeOffset agora)
    {
        Status = StatusAssinatura.Ativa;
        InicioTolerancia = null;
        ProximaCobranca = agora.AddMonths(1);
        FimCicloPago = agora.AddMonths(1);
        UpdatedAt = agora;
    }

    public void ReativarAposPagamento(DateTimeOffset agora)
    {
        if (Status is not (StatusAssinatura.Suspensa or StatusAssinatura.PendentePagamento))
            throw new InvalidOperationException("Somente assinatura suspensa ou pendente pode ser reativada.");
        AtivarAposPagamento(agora);
    }

    public PagamentoPlano CriarPagamentoPix(
        Guid planoDestinoId,
        decimal valor,
        DateTimeOffset vencimento,
        string txId)
    {
        foreach (var pendente in _pagamentos.Where(p => p.Status == StatusPagamentoPlano.Pendente))
            pendente.Cancelar("Substituído por nova cobrança PIX");

        var pagamento = PagamentoPlano.CriarPix(TenantId, Id, planoDestinoId, valor, vencimento, txId);
        _pagamentos.Add(pagamento);
        UpdatedAt = DateTimeOffset.UtcNow;
        return pagamento;
    }
}

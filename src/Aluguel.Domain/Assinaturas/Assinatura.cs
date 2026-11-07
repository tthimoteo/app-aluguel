using Aluguel.Domain.Common;

namespace Aluguel.Domain.Assinaturas;

/// <summary>Assinatura do cliente com máquina de estados (§3).</summary>
public class Assinatura : AggregateRoot, ITenantOwned, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid PlanoId { get; private set; }
    /// <summary>Downgrade agendado para a próxima renovação (sem estorno/crédito).</summary>
    public Guid? PlanoAgendadoId { get; private set; }
    public StatusAssinatura Status { get; private set; } = StatusAssinatura.Trial;
    public DateTimeOffset DataInicio { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DataFimTrial { get; private set; }
    public DateTimeOffset? ProximaCobranca { get; private set; }
    public DateTimeOffset? InicioTolerancia { get; private set; }
    /// <summary>Momento em que o cancelamento passou a valer (fim do ciclo).</summary>
    public DateTimeOffset? CanceladaEm { get; private set; }
    /// <summary>Pedido de cancelamento — acesso permanece até <see cref="FimCicloPago"/>.</summary>
    public DateTimeOffset? CancelamentoSolicitadoEm { get; private set; }
    public DateTimeOffset? FimCicloPago { get; private set; }
    public decimal ValorMensal { get; private set; }
    /// <summary>Legado Mercado Pago — mantido para compatibilidade de schema.</summary>
    public string? MercadoPagoSubscriptionId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public bool CancelamentoPendente => CancelamentoSolicitadoEm is not null && Status == StatusAssinatura.Ativa;
    public bool DowngradeAgendado => PlanoAgendadoId is not null;

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
    /// Primeira contratação (Trial → pago): fica pendente até o PIX.
    /// </summary>
    public void IniciarPrimeiraContratacao(Guid planoDestinoId, decimal valorMensal)
    {
        GarantirNaoCanceladaDefinitiva();
        PlanoId = planoDestinoId;
        ValorMensal = valorMensal;
        PlanoAgendadoId = null;
        CancelamentoSolicitadoEm = null;
        Status = StatusAssinatura.PendentePagamento;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Upgrade imediato: gera cobrança do novo valor; o plano vigente só muda após o PIX.
    /// Cancela downgrade/cancelamento pendentes.
    /// </summary>
    public void RegistrarIntencaoUpgrade()
    {
        GarantirNaoCanceladaDefinitiva();
        if (Status is not (StatusAssinatura.Ativa or StatusAssinatura.PendentePagamento or StatusAssinatura.Trial))
            throw new InvalidOperationException("Upgrade só é permitido com assinatura ativa, trial ou pendente.");

        PlanoAgendadoId = null;
        CancelamentoSolicitadoEm = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Downgrade agendado para a próxima renovação. Sem cobrança agora, sem estorno/crédito.
    /// </summary>
    public void AgendarDowngrade(Guid planoDestinoId)
    {
        GarantirNaoCanceladaDefinitiva();
        if (Status != StatusAssinatura.Ativa)
            throw new InvalidOperationException("Downgrade só pode ser agendado com assinatura ativa.");

        if (CancelamentoSolicitadoEm is not null)
            throw new InvalidOperationException(
                "Há um cancelamento solicitado. Revogue o cancelamento antes de agendar downgrade.");

        if (planoDestinoId == PlanoId)
            throw new InvalidOperationException("O plano informado já é o plano vigente.");

        PlanoAgendadoId = planoDestinoId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CancelarDowngradeAgendado()
    {
        if (PlanoAgendadoId is null) return;
        PlanoAgendadoId = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Cancelamento: mantém acesso até o fim do ciclo já pago; depois o job bloqueia.
    /// </summary>
    public void SolicitarCancelamento(DateTimeOffset agora)
    {
        GarantirNaoCanceladaDefinitiva();
        if (Status is not (StatusAssinatura.Ativa or StatusAssinatura.PendentePagamento))
            throw new InvalidOperationException("Somente assinatura ativa ou pendente pode ser cancelada.");

        if (FimCicloPago is null && Status == StatusAssinatura.Ativa)
            throw new InvalidOperationException("Assinatura ativa sem ciclo pago definido.");

        PlanoAgendadoId = null;
        CancelamentoSolicitadoEm = agora;
        UpdatedAt = agora;
    }

    public void RevogarCancelamento()
    {
        if (CancelamentoSolicitadoEm is null) return;
        if (Status == StatusAssinatura.Cancelada)
            throw new InvalidOperationException("Cancelamento já efetivado não pode ser revogado.");

        CancelamentoSolicitadoEm = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Aplica o plano agendado na renovação e devolve o id efetivo do ciclo.</summary>
    public Guid AplicarDowngradeAgendadoSeHouver(Guid planoEfetivoId, decimal valorMensal)
    {
        PlanoId = planoEfetivoId;
        ValorMensal = valorMensal;
        PlanoAgendadoId = null;
        UpdatedAt = DateTimeOffset.UtcNow;
        return PlanoId;
    }

    /// <summary>Inicia renovação mensal aguardando PIX (recorrência).</summary>
    public void IniciarRenovacaoAguardandoPagamento(DateTimeOffset agora)
    {
        if (CancelamentoSolicitadoEm is not null)
            throw new InvalidOperationException("Assinatura com cancelamento solicitado não gera renovação.");

        if (Status != StatusAssinatura.Ativa)
            throw new InvalidOperationException("Somente assinatura ativa entra em renovação.");

        Status = StatusAssinatura.PendentePagamento;
        InicioTolerancia = agora;
        UpdatedAt = agora;
    }

    /// <summary>Ativa/reinicia o ciclo após liquidação PIX (contratação, upgrade ou renovação).</summary>
    public void AtivarAposPagamento(DateTimeOffset agora, Guid planoPagoId, decimal valorPago)
    {
        PlanoId = planoPagoId;
        ValorMensal = valorPago;
        PlanoAgendadoId = null;
        CancelamentoSolicitadoEm = null;
        Status = StatusAssinatura.Ativa;
        InicioTolerancia = null;
        CanceladaEm = null;
        ProximaCobranca = agora.AddMonths(1);
        FimCicloPago = agora.AddMonths(1);
        UpdatedAt = agora;
    }

    public void ReativarAposPagamento(DateTimeOffset agora, Guid planoPagoId, decimal valorPago)
    {
        if (Status is not (StatusAssinatura.Suspensa or StatusAssinatura.PendentePagamento or StatusAssinatura.Ativa))
            throw new InvalidOperationException("Somente assinatura suspensa, pendente ou ativa pode ser reativada via pagamento.");
        AtivarAposPagamento(agora, planoPagoId, valorPago);
    }

    /// <summary>Fim do período pago após cancelamento solicitado → Cancelada.</summary>
    public void EfetivarCancelamentoAoFimDoCiclo(DateTimeOffset agora)
    {
        if (CancelamentoSolicitadoEm is null)
            throw new InvalidOperationException("Não há cancelamento solicitado.");

        Status = StatusAssinatura.Cancelada;
        CanceladaEm = agora;
        ProximaCobranca = null;
        InicioTolerancia = null;
        PlanoAgendadoId = null;
        UpdatedAt = agora;
    }

    public void SuspenderPorInadimplencia(DateTimeOffset agora)
    {
        if (Status is not (StatusAssinatura.PendentePagamento or StatusAssinatura.Ativa))
            return;

        Status = StatusAssinatura.Suspensa;
        UpdatedAt = agora;
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

    private void GarantirNaoCanceladaDefinitiva()
    {
        if (Status == StatusAssinatura.Cancelada)
            throw new InvalidOperationException("Assinatura cancelada. Contrate um novo plano para reativar.");
    }
}

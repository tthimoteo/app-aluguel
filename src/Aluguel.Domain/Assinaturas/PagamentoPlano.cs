using Aluguel.Domain.Common;

namespace Aluguel.Domain.Assinaturas;

/// <summary>Cobrança/pagamento de um ciclo da assinatura (PIX via Banco Inter).</summary>
public class PagamentoPlano : Entity, ITenantOwned, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid AssinaturaId { get; private set; }
    /// <summary>Plano que será aplicado ao cliente após a liquidação deste pagamento.</summary>
    public Guid PlanoDestinoId { get; private set; }
    public decimal Valor { get; private set; }
    public DateTimeOffset DataVencimento { get; private set; }
    public DateTimeOffset? DataPagamento { get; private set; }
    public StatusPagamentoPlano Status { get; private set; } = StatusPagamentoPlano.Pendente;
    public MetodoPagamento? MetodoPagamento { get; private set; }
    /// <summary>Legado Mercado Pago — mantido para compatibilidade de schema.</summary>
    public string? MercadoPagoPaymentId { get; private set; }
    public string? Observacao { get; private set; }

    /// <summary>Identificador da cobrança PIX no Banco Inter (txid, 26–35 alfanuméricos).</summary>
    public string? PixTxId { get; private set; }
    /// <summary>Payload EMV copia-e-cola do QR Code dinâmico.</summary>
    public string? PixCopiaCola { get; private set; }
    /// <summary>Imagem do QR em base64 (quando disponível).</summary>
    public string? PixQrCodeBase64 { get; private set; }
    /// <summary>endToEndId informado no webhook de liquidação.</summary>
    public string? PixEndToEndId { get; private set; }
    public string? PixLocation { get; private set; }

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

    public static PagamentoPlano CriarPix(
        Guid tenantId,
        Guid assinaturaId,
        Guid planoDestinoId,
        decimal valor,
        DateTimeOffset dataVencimento,
        string txId)
    {
        if (string.IsNullOrWhiteSpace(txId) || txId.Length is < 26 or > 35)
            throw new ArgumentException("txid PIX deve ter entre 26 e 35 caracteres alfanuméricos.", nameof(txId));

        return new PagamentoPlano
        {
            TenantId = tenantId,
            AssinaturaId = assinaturaId,
            PlanoDestinoId = planoDestinoId,
            Valor = valor,
            DataVencimento = dataVencimento,
            Status = StatusPagamentoPlano.Pendente,
            MetodoPagamento = Assinaturas.MetodoPagamento.PIX,
            PixTxId = txId,
        };
    }

    public void RegistrarQrCode(string pixCopiaCola, string? qrCodeBase64 = null, string? location = null)
    {
        if (Status != StatusPagamentoPlano.Pendente)
            throw new InvalidOperationException("Somente pagamento pendente pode receber QR Code.");

        PixCopiaCola = pixCopiaCola;
        PixQrCodeBase64 = qrCodeBase64;
        PixLocation = location;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ConfirmarPagamentoPix(string endToEndId, DateTimeOffset quando)
    {
        if (Status == StatusPagamentoPlano.Pago)
            return; // idempotente

        if (Status != StatusPagamentoPlano.Pendente)
            throw new InvalidOperationException($"Pagamento em status {Status} não pode ser confirmado.");

        if (string.IsNullOrWhiteSpace(endToEndId))
            throw new ArgumentException("endToEndId é obrigatório.", nameof(endToEndId));

        Status = StatusPagamentoPlano.Pago;
        DataPagamento = quando;
        PixEndToEndId = endToEndId.Trim();
        Observacao = "Liquidado via PIX Banco Inter";
        UpdatedAt = quando;
    }

    public void Cancelar(string? motivo)
    {
        if (Status != StatusPagamentoPlano.Pendente)
            return;

        Status = StatusPagamentoPlano.Falhou;
        Observacao = motivo;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

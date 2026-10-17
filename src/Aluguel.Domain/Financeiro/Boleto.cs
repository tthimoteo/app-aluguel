using Aluguel.Domain.Common;

namespace Aluguel.Domain.Financeiro;

/// <summary>Boleto de cobrança de aluguel de uma competência (PDF no storage quando gerado).</summary>
public class Boleto : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ImovelId { get; private set; }
    public Guid? NfseId { get; private set; }
    public string Competencia { get; private set; } = default!;
    public decimal Valor { get; private set; }
    public DateOnly DataVencimento { get; private set; }
    public string? NossoNumero { get; private set; }
    public string? LinhaDigitavel { get; private set; }
    public StatusBoleto Status { get; private set; } = StatusBoleto.Gerado;
    public string? StoragePathPdf { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Boleto() { }

    public Boleto(Guid tenantId, Guid clienteId, Guid imovelId, string competencia, decimal valor,
        DateOnly dataVencimento, Guid? nfseId = null, string? nossoNumero = null, string? linhaDigitavel = null)
    {
        if (string.IsNullOrWhiteSpace(competencia))
            throw new ArgumentException("Competência é obrigatória.", nameof(competencia));

        TenantId = tenantId;
        ClienteId = clienteId;
        ImovelId = imovelId;
        Competencia = competencia.Trim();
        Valor = valor;
        DataVencimento = dataVencimento;
        NfseId = nfseId;
        NossoNumero = nossoNumero;
        LinhaDigitavel = linhaDigitavel;
    }

    public void DefinirPdf(string storagePath)
    {
        if (string.IsNullOrWhiteSpace(storagePath))
            throw new ArgumentException("Caminho do PDF é obrigatório.", nameof(storagePath));
        StoragePathPdf = storagePath.Trim();
        Status = StatusBoleto.Gerado;
    }

    public void MarcarPago() => Status = StatusBoleto.Pago;

    public bool PossuiPdf => !string.IsNullOrWhiteSpace(StoragePathPdf);
}

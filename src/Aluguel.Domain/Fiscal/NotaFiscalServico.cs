using Aluguel.Domain.Common;

namespace Aluguel.Domain.Fiscal;

/// <summary>Faturamento / NFS-e de uma competência (§11). 1 não-cancelada por competência (CASO 6/7/8).</summary>
public class NotaFiscalServico : AggregateRoot, ITenantOwned, IAuditable
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ImovelId { get; private set; }
    public Guid? ContratoId { get; private set; }
    public Guid? InquilinoId { get; private set; }
    public string Competencia { get; private set; } = default!;   // "MM/AAAA"
    public long? Numero { get; private set; }
    public string? Serie { get; private set; }
    public string? ChaveAcesso { get; private set; }
    public StatusNfse Status { get; private set; } = StatusNfse.Rascunho;
    public decimal ValorServico { get; private set; }
    public decimal Desconto { get; private set; }
    public decimal Multa { get; private set; }
    public decimal Juros { get; private set; }
    public decimal ValorFaturado { get; private set; }
    public DateTimeOffset? DataEmissao { get; private set; }
    public Guid? UsuarioEmissorId { get; private set; }
    public DateTimeOffset? SolicitadoEm { get; private set; }
    public string? MotivoRejeicao { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public string? ProtocoloCancelamento { get; private set; }
    public DateTimeOffset? DataCancelamento { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private readonly List<DocumentoFiscal> _documentos = new();
    public IReadOnlyCollection<DocumentoFiscal> Documentos => _documentos.AsReadOnly();

    private NotaFiscalServico() { }

    public NotaFiscalServico(Guid tenantId, Guid clienteId, Guid imovelId, string competencia,
        decimal valorServico, Guid? contratoId = null, Guid? inquilinoId = null,
        decimal desconto = 0, decimal multa = 0, decimal juros = 0)
    {
        TenantId = tenantId;
        ClienteId = clienteId;
        ImovelId = imovelId;
        Competencia = competencia;
        ValorServico = valorServico;
        ContratoId = contratoId;
        InquilinoId = inquilinoId;
        Desconto = desconto;
        Multa = multa;
        Juros = juros;
        ValorFaturado = valorServico - desconto + multa + juros;
    }
}

using Aluguel.Domain.Common;

namespace Aluguel.Domain.Financeiro;

/// <summary>Despesa do imóvel: IPTU ou outras (§12/§13 Contas a Pagar).</summary>
public class Despesa : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid ImovelId { get; private set; }
    public TipoDespesa Tipo { get; private set; }
    public string? Descricao { get; private set; }
    public string? Categoria { get; private set; }
    public string? Fornecedor { get; private set; }
    public string Competencia { get; private set; } = default!;   // "MM/AAAA"
    public decimal Valor { get; private set; }
    public DateOnly? DataPagamento { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Despesa() { }

    public Despesa(Guid tenantId, Guid clienteId, Guid imovelId, TipoDespesa tipo, string competencia,
        decimal valor, string? descricao = null, string? categoria = null, string? fornecedor = null,
        DateOnly? dataPagamento = null)
    {
        TenantId = tenantId;
        ClienteId = clienteId;
        ImovelId = imovelId;
        Tipo = tipo;
        Competencia = competencia;
        Valor = valor;
        Descricao = descricao;
        Categoria = categoria;
        Fornecedor = fornecedor;
        DataPagamento = dataPagamento;
    }
}

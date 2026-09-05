using Aluguel.Domain.Common;

namespace Aluguel.Domain.Assinaturas;

/// <summary>Trilha de auditoria específica das operações de assinatura (§3).</summary>
public class AuditoriaAssinatura : Entity, ITenantOwned
{
    public Guid TenantId { get; private set; }
    public Guid ClienteId { get; private set; }
    public Guid? AssinaturaId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public EventoAssinatura Evento { get; private set; }
    public DateTimeOffset DataHora { get; private set; } = DateTimeOffset.UtcNow;
    public string? Ip { get; private set; }
    public string? PlanoAnterior { get; private set; }
    public string? NovoPlano { get; private set; }
    public decimal? Valor { get; private set; }
    public string? Descricao { get; private set; }

    private AuditoriaAssinatura() { }

    public AuditoriaAssinatura(Guid tenantId, Guid clienteId, EventoAssinatura evento,
        Guid? assinaturaId = null, Guid? usuarioId = null, string? ip = null,
        string? planoAnterior = null, string? novoPlano = null, decimal? valor = null, string? descricao = null)
    {
        TenantId = tenantId;
        ClienteId = clienteId;
        Evento = evento;
        AssinaturaId = assinaturaId;
        UsuarioId = usuarioId;
        Ip = ip;
        PlanoAnterior = planoAnterior;
        NovoPlano = novoPlano;
        Valor = valor;
        Descricao = descricao;
    }
}

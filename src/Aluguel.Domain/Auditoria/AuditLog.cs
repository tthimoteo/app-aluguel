namespace Aluguel.Domain.Auditoria;

/// <summary>Trilha de auditoria genérica (§13): login, emissão, cancelamento, pagamento, despesas, alteração cadastral.</summary>
public class AuditLog
{
    public long Id { get; private set; }
    public Guid? TenantId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string Acao { get; private set; } = default!;
    public string? Tabela { get; private set; }
    public Guid? RegistroId { get; private set; }
    public string? ValoresAntes { get; private set; }
    public string? ValoresDepois { get; private set; }
    public DateTimeOffset DataHora { get; private set; } = DateTimeOffset.UtcNow;
    public string? Ip { get; private set; }

    private AuditLog() { }

    public AuditLog(Guid? tenantId, Guid? usuarioId, string acao, string? tabela = null,
        Guid? registroId = null, string? valoresAntes = null, string? valoresDepois = null, string? ip = null)
    {
        TenantId = tenantId;
        UsuarioId = usuarioId;
        Acao = acao;
        Tabela = tabela;
        RegistroId = registroId;
        ValoresAntes = valoresAntes;
        ValoresDepois = valoresDepois;
        Ip = ip;
    }
}

/// <summary>Ações auditáveis conhecidas.</summary>
public static class AcaoAuditoria
{
    public const string Login = "Login";
    public const string AlteracaoCadastral = "AlteracaoCadastral";
    public const string Emissao = "Emissao";
    public const string Cancelamento = "Cancelamento";
    public const string Pagamento = "Pagamento";
    public const string Despesa = "Despesa";
}

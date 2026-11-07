namespace Aluguel.Domain.Auditoria;

/// <summary>Trilha de auditoria genérica (§13): cadastros, cobrança, pagamento e alterações.</summary>
public class AuditLog
{
    public long Id { get; private set; }
    public Guid? TenantId { get; private set; }
    /// <summary>Cliente (proprietário) ao qual o evento se refere — usado no filtro por escopo.</summary>
    public Guid? ClienteId { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string? UsuarioNome { get; private set; }
    public string Acao { get; private set; } = default!;
    public string? Tabela { get; private set; }
    public Guid? RegistroId { get; private set; }
    public string? Resumo { get; private set; }
    public string? ValoresAntes { get; private set; }
    public string? ValoresDepois { get; private set; }
    public DateTimeOffset DataHora { get; private set; } = DateTimeOffset.UtcNow;
    public string? Ip { get; private set; }

    private AuditLog() { }

    public AuditLog(
        Guid? tenantId,
        Guid? usuarioId,
        string acao,
        string? tabela = null,
        Guid? registroId = null,
        string? valoresAntes = null,
        string? valoresDepois = null,
        string? ip = null,
        Guid? clienteId = null,
        string? usuarioNome = null,
        string? resumo = null)
    {
        if (string.IsNullOrWhiteSpace(acao))
            throw new ArgumentException("Ação é obrigatória.", nameof(acao));

        TenantId = tenantId;
        ClienteId = clienteId;
        UsuarioId = usuarioId;
        UsuarioNome = string.IsNullOrWhiteSpace(usuarioNome) ? null : usuarioNome.Trim();
        Acao = acao.Trim();
        Tabela = string.IsNullOrWhiteSpace(tabela) ? null : tabela.Trim();
        RegistroId = registroId;
        Resumo = string.IsNullOrWhiteSpace(resumo) ? null : resumo.Trim();
        ValoresAntes = valoresAntes;
        ValoresDepois = valoresDepois;
        Ip = ip;
    }
}

/// <summary>Ações auditáveis conhecidas.</summary>
public static class AcaoAuditoria
{
    public const string Login = "Login";
    public const string Cadastro = "Cadastro";
    public const string Alteracao = "Alteracao";
    public const string Remocao = "Remocao";
    public const string AlteracaoCadastral = "AlteracaoCadastral";
    public const string Emissao = "Emissao";
    public const string Cancelamento = "Cancelamento";
    public const string Pagamento = "Pagamento";
    public const string Despesa = "Despesa";
}

/// <summary>Nomes estáveis de tabela/entidade nos logs.</summary>
public static class TabelaAuditoria
{
    public const string Imovel = "imovel";
    public const string Inquilino = "inquilino";
    public const string Contrato = "contrato";
    public const string Cliente = "cliente";
    public const string Usuario = "usuario";
    public const string Cobranca = "cobranca";
    public const string Pagamento = "pagamento";
}

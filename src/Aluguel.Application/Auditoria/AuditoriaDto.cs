namespace Aluguel.Application.Auditoria;

/// <summary>Item da consulta de auditoria (API/UI).</summary>
public sealed record AuditoriaDto(
    long Id,
    Guid? ClienteId,
    Guid? UsuarioId,
    string? UsuarioNome,
    string Acao,
    string? Tabela,
    Guid? RegistroId,
    string? Resumo,
    string? ValoresAntes,
    string? ValoresDepois,
    DateTimeOffset DataHora,
    string? Ip);

/// <summary>Rótulos amigáveis para tabela/ação na UI.</summary>
public static class AuditoriaRotulos
{
    public static string Tabela(string? tabela) => tabela switch
    {
        Domain.Auditoria.TabelaAuditoria.Imovel => "Imóvel",
        Domain.Auditoria.TabelaAuditoria.Inquilino => "Inquilino",
        Domain.Auditoria.TabelaAuditoria.Contrato => "Contrato",
        Domain.Auditoria.TabelaAuditoria.Cliente => "Cliente",
        Domain.Auditoria.TabelaAuditoria.Usuario => "Usuário",
        Domain.Auditoria.TabelaAuditoria.Cobranca => "Cobrança",
        Domain.Auditoria.TabelaAuditoria.Pagamento => "Pagamento",
        _ => tabela ?? "—",
    };

    public static string Acao(string? acao) => acao switch
    {
        Domain.Auditoria.AcaoAuditoria.Cadastro => "Cadastro",
        Domain.Auditoria.AcaoAuditoria.Alteracao => "Alteração",
        Domain.Auditoria.AcaoAuditoria.Remocao => "Remoção",
        Domain.Auditoria.AcaoAuditoria.Pagamento => "Pagamento",
        Domain.Auditoria.AcaoAuditoria.Emissao => "Emissão",
        Domain.Auditoria.AcaoAuditoria.Cancelamento => "Cancelamento",
        Domain.Auditoria.AcaoAuditoria.Login => "Login",
        Domain.Auditoria.AcaoAuditoria.Despesa => "Despesa",
        Domain.Auditoria.AcaoAuditoria.AlteracaoCadastral => "Alteração cadastral",
        _ => acao ?? "—",
    };
}

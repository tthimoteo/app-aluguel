namespace Aluguel.Application.Abstractions;

/// <summary>Registra e consulta a trilha de auditoria (§13).</summary>
public interface IAuditoriaService
{
    /// <summary>
    /// Persiste um evento de auditoria (SaveChanges próprio).
    /// Captura usuário/tenant/IP do contexto da requisição.
    /// </summary>
    Task RegistrarAsync(
        string acao,
        string tabela,
        Guid? registroId,
        Guid? clienteId,
        object? valoresAntes,
        object? valoresDepois,
        string? resumo = null,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<AuditoriaRegistro> Itens, int Total)> ListarAsync(
        Guid? clienteId,
        string? tabela,
        string? acao,
        DateTimeOffset? de,
        DateTimeOffset? ate,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}

/// <summary>Projeção de leitura de um log de auditoria.</summary>
public sealed record AuditoriaRegistro(
    long Id,
    Guid? TenantId,
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

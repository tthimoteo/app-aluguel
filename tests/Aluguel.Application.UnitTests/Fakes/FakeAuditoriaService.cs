using Aluguel.Application.Abstractions;

namespace Aluguel.Application.UnitTests.Fakes;

public sealed class FakeAuditoriaService : IAuditoriaService
{
    public List<Registro> Registros { get; } = [];

    public Task RegistrarAsync(
        string acao,
        string tabela,
        Guid? registroId,
        Guid? clienteId,
        object? valoresAntes,
        object? valoresDepois,
        string? resumo = null,
        CancellationToken cancellationToken = default)
    {
        Registros.Add(new Registro(acao, tabela, registroId, clienteId, valoresAntes, valoresDepois, resumo));
        return Task.CompletedTask;
    }

    public Task<(IReadOnlyList<AuditoriaRegistro> Itens, int Total)> ListarAsync(
        Guid? clienteId,
        string? tabela,
        string? acao,
        DateTimeOffset? de,
        DateTimeOffset? ate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var filtrados = Registros
            .Where(r => clienteId is null || r.ClienteId == clienteId)
            .Where(r => tabela is null || r.Tabela == tabela)
            .Where(r => acao is null || r.Acao == acao)
            .ToList();
        var pagina = filtrados
            .Skip(skip)
            .Take(take)
            .Select((r, i) => new AuditoriaRegistro(
                i + 1, null, r.ClienteId, null, null, r.Acao, r.Tabela, r.RegistroId, r.Resumo,
                null, null, DateTimeOffset.UtcNow, null))
            .ToList();
        return Task.FromResult<(IReadOnlyList<AuditoriaRegistro>, int)>((pagina, filtrados.Count));
    }

    public sealed record Registro(
        string Acao,
        string Tabela,
        Guid? RegistroId,
        Guid? ClienteId,
        object? ValoresAntes,
        object? ValoresDepois,
        string? Resumo);
}

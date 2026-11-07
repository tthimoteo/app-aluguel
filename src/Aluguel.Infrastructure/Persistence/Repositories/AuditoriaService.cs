using System.Text.Json;
using System.Text.Json.Serialization;
using Aluguel.Application.Abstractions;
using Aluguel.Domain.Auditoria;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

public sealed class AuditoriaService(
    AppDbContext db,
    ICurrentUser currentUser,
    ICurrentTenant tenant,
    IHttpContextAccessor http) : IAuditoriaService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task RegistrarAsync(
        string acao,
        string tabela,
        Guid? registroId,
        Guid? clienteId,
        object? valoresAntes,
        object? valoresDepois,
        string? resumo = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog(
            tenantId: tenant.TenantId,
            usuarioId: currentUser.UserId,
            acao: acao,
            tabela: tabela,
            registroId: registroId,
            valoresAntes: Serializar(valoresAntes),
            valoresDepois: Serializar(valoresDepois),
            ip: ResolverIp(),
            clienteId: clienteId ?? currentUser.ClienteId,
            usuarioNome: currentUser.Nome,
            resumo: resumo);

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<AuditoriaRegistro> Itens, int Total)> ListarAsync(
        Guid? clienteId,
        string? tabela,
        string? acao,
        DateTimeOffset? de,
        DateTimeOffset? ate,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var q = db.AuditLogs.AsNoTracking().AsQueryable();

        if (clienteId is { } c)
            q = q.Where(x => x.ClienteId == c);

        if (!string.IsNullOrWhiteSpace(tabela))
            q = q.Where(x => x.Tabela == tabela);

        if (!string.IsNullOrWhiteSpace(acao))
            q = q.Where(x => x.Acao == acao);

        if (de is { } d0)
            q = q.Where(x => x.DataHora >= d0);

        if (ate is { } d1)
            q = q.Where(x => x.DataHora <= d1);

        var total = await q.CountAsync(cancellationToken);
        var itens = await q
            .OrderByDescending(x => x.DataHora)
            .ThenByDescending(x => x.Id)
            .Skip(skip)
            .Take(take)
            .Select(x => new AuditoriaRegistro(
                x.Id,
                x.TenantId,
                x.ClienteId,
                x.UsuarioId,
                x.UsuarioNome,
                x.Acao,
                x.Tabela,
                x.RegistroId,
                x.Resumo,
                x.ValoresAntes,
                x.ValoresDepois,
                x.DataHora,
                x.Ip))
            .ToListAsync(cancellationToken);

        return (itens, total);
    }

    private static string? Serializar(object? valor)
    {
        if (valor is null) return null;
        if (valor is string s) return string.IsNullOrWhiteSpace(s) ? null : s;
        return JsonSerializer.Serialize(valor, JsonOpts);
    }

    private string? ResolverIp()
    {
        var ctx = http.HttpContext;
        if (ctx is null) return null;

        var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwarded))
        {
            var primeiro = forwarded.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(primeiro))
                return Truncar(primeiro, 60);
        }

        var ip = ctx.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(ip) ? null : Truncar(ip, 60);
    }

    private static string Truncar(string valor, int max) =>
        valor.Length <= max ? valor : valor[..max];
}

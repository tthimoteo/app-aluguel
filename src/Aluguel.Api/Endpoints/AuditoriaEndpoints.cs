using Aluguel.Application.Auditoria.ListarAuditoria;
using MediatR;

namespace Aluguel.Api.Endpoints;

public static class AuditoriaEndpoints
{
    public static IEndpointRouteBuilder MapAuditoriaEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/auditoria")
            .WithTags("Auditoria")
            .RequireAuthorization();

        grupo.MapGet("/", async (
            Guid? clienteId,
            string? tabela,
            string? acao,
            DateTimeOffset? de,
            DateTimeOffset? ate,
            int? skip,
            int? take,
            ISender sender,
            CancellationToken ct) =>
        {
            var pagina = await sender.Send(new ListarAuditoriaQuery(
                clienteId, tabela, acao, de, ate, skip ?? 0, take ?? 50), ct);
            return Results.Ok(pagina);
        })
        .WithName("ListarAuditoria");

        return app;
    }
}
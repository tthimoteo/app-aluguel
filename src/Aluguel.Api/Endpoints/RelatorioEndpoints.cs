using Aluguel.Application.Relatorios.ObterPainelRelatorios;
using MediatR;

namespace Aluguel.Api.Endpoints;

public static class RelatorioEndpoints
{
    public static IEndpointRouteBuilder MapRelatorioEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/relatorios")
            .WithTags("Relatórios")
            .RequireAuthorization();

        grupo.MapGet("/painel", async (
            int? ano,
            int? mes,
            DateOnly? dataInicio,
            DateOnly? dataFim,
            Guid? clienteId,
            Guid? imovelId,
            int? diasAtrasoMinimos,
            ISender sender,
            CancellationToken ct) =>
        {
            try
            {
                var dto = await sender.Send(new ObterPainelRelatoriosQuery(
                    ano, mes, dataInicio, dataFim, clienteId, imovelId, diasAtrasoMinimos), ct);
                return Results.Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        })
        .WithName("ObterPainelRelatorios");

        return app;
    }
}

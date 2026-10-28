using Aluguel.Application.Autorizacao;
using Aluguel.Application.Boletos;
using Aluguel.Application.Boletos.ListarProvedoresBoleto;
using Aluguel.Application.Boletos.ObterIntegracaoBoletoCliente;
using Aluguel.Application.Boletos.SalvarIntegracaoBoletoCliente;
using MediatR;

namespace Aluguel.Api.Endpoints;

public sealed record SalvarIntegracaoBoletoRequest(
    string ProvedorCodigo,
    Dictionary<string, string?>? Campos);

public static class BoletoEndpoints
{
    public static IEndpointRouteBuilder MapBoletoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/boleto-provedores")
            .WithTags("Boletos")
            .RequireAuthorization();

        grupo.MapGet("/", async (ISender sender, CancellationToken ct) =>
        {
            var lista = await sender.Send(new ListarProvedoresBoletoQuery(), ct);
            return Results.Ok(lista);
        })
        .WithName("ListarProvedoresBoleto");

        var cliente = app.MapGroup("/api/clientes/{id:guid}/integracao-boleto")
            .WithTags("Boletos")
            .RequireAuthorization();

        cliente.MapGet("/", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new ObterIntegracaoBoletoClienteQuery(id), ct);
            return dto is null
                ? Results.NotFound(new { erro = "Nenhuma integração de boleto ativa para este cliente." })
                : Results.Ok(dto);
        })
        .WithName("ObterIntegracaoBoletoCliente");

        cliente.MapPut("/", async (Guid id, SalvarIntegracaoBoletoRequest req, ISender sender, CancellationToken ct) =>
        {
            try
            {
                var dto = await sender.Send(new SalvarIntegracaoBoletoClienteCommand(
                    id,
                    req.ProvedorCodigo,
                    req.Campos ?? new Dictionary<string, string?>()), ct);
                return Results.Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        })
        .WithName("SalvarIntegracaoBoletoCliente")
        .RequireAuthorization(Politicas.GerenciaUsuarios);

        return app;
    }
}

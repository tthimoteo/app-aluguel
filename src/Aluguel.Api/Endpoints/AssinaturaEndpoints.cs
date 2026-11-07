using Aluguel.Application.Assinaturas.ContratarPlano;
using Aluguel.Application.Assinaturas.ObterPagamentoPlano;
using Aluguel.Application.Assinaturas.ProcessarWebhookPixInter;
using Aluguel.Application.Assinaturas.SimularPagamentoPix;
using Aluguel.Application.Autorizacao;
using MediatR;

namespace Aluguel.Api.Endpoints;

public static class AssinaturaEndpoints
{
    public static IEndpointRouteBuilder MapAssinaturaEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/assinaturas")
            .WithTags("Assinaturas")
            .RequireAuthorization();

        grupo.MapPost("/contratar", async (ContratarPlanoRequest req, ISender sender, CancellationToken ct) =>
            {
                var resultado = await sender.Send(new ContratarPlanoCommand(req.PlanoId), ct);
                return Results.Ok(resultado);
            })
            .RequireAuthorization(p => p.RequireRole(Perfis.Gestor))
            .WithName("ContratarPlanoPix");

        grupo.MapGet("/pagamentos/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var dto = await sender.Send(new ObterPagamentoPlanoQuery(id), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            })
            .WithName("ObterPagamentoPlanoPix");

        // Simulação do webhook Inter — apenas Development (Mock).
        grupo.MapPost("/pagamentos/{id:guid}/simular-pagamento", async (
                Guid id, ISender sender, IHostEnvironment env, CancellationToken ct) =>
            {
                if (!env.IsDevelopment())
                    return Results.NotFound();
                var r = await sender.Send(new SimularPagamentoPixCommand(id), ct);
                return Results.Ok(r);
            })
            .RequireAuthorization(p => p.RequireRole(Perfis.Gestor, Perfis.Administrador))
            .WithName("SimularPagamentoPixInter");

        // Webhook público chamado pelo Banco Inter na liquidação do PIX.
        app.MapPost("/api/webhooks/banco-inter/pix", async (
                HttpRequest http, ISender sender, CancellationToken ct) =>
            {
                using var reader = new StreamReader(http.Body);
                var payload = await reader.ReadToEndAsync(ct);
                var r = await sender.Send(new ProcessarWebhookPixInterCommand(payload), ct);
                return Results.Ok(r);
            })
            .AllowAnonymous()
            .WithTags("Webhooks")
            .WithName("WebhookPixBancoInter");

        return app;
    }
}

public sealed record ContratarPlanoRequest(Guid PlanoId);

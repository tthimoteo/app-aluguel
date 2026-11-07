using Aluguel.Application.CadastroPublico.RegistrarContaPublica;
using Aluguel.Application.CadastroPublico.SolicitarDemonstracao;
using MediatR;

namespace Aluguel.Api.Endpoints;

public sealed record RegistrarContaPublicaRequest(
    string Nome,
    string Email,
    string Telefone,
    string Cpf,
    string Senha,
    Guid? PlanoId);

public sealed record SolicitarDemonstracaoRequest(
    string Nome,
    string Email,
    string Telefone,
    string? Mensagem);

public static class CadastroPublicoEndpoints
{
    public static IEndpointRouteBuilder MapCadastroPublicoEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/cadastro-publico")
            .WithTags("Cadastro público")
            .AllowAnonymous();

        grupo.MapPost("/", async (RegistrarContaPublicaRequest req, ISender sender, CancellationToken ct) =>
            {
                var r = await sender.Send(new RegistrarContaPublicaCommand(
                    req.Nome, req.Email, req.Telefone, req.Cpf, req.Senha, req.PlanoId), ct);
                return Results.Ok(r);
            })
            .WithName("RegistrarContaPublica");

        grupo.MapPost("/demonstracao", async (
                SolicitarDemonstracaoRequest req, ISender sender, CancellationToken ct) =>
            {
                var r = await sender.Send(new SolicitarDemonstracaoCommand(
                    req.Nome, req.Email, req.Telefone, req.Mensagem), ct);
                return Results.Ok(r);
            })
            .WithName("SolicitarDemonstracao");

        return app;
    }
}

using Aluguel.Application.Abstractions;
using Aluguel.Application.Autorizacao;
using Aluguel.Application.Clientes;
using Aluguel.Application.Imoveis.AtualizarImovel;
using Aluguel.Application.Imoveis.CriarImovel;
using Aluguel.Application.Imoveis.ListarImoveis;
using Aluguel.Application.Imoveis.ObterImovelPorId;
using Aluguel.Application.Imoveis.RemoverImovel;
using Aluguel.Domain.Common;
using Aluguel.Domain.Imoveis;
using MediatR;

namespace Aluguel.Api.Endpoints;

public sealed record CriarImovelRequest(
    Guid? ClienteId,
    string Nome,
    TipoImovel Tipo,
    string? NumeroIptu,
    string? NumeroMatricula,
    EnderecoInput? Endereco);

public sealed record AtualizarImovelRequest(
    string Nome,
    TipoImovel Tipo,
    string? NumeroIptu,
    string? NumeroMatricula,
    StatusAtivoInativo Status,
    EnderecoInput? Endereco);

public static class ImovelEndpoints
{
    public static IEndpointRouteBuilder MapImovelEndpoints(this IEndpointRouteBuilder app)
    {
        // Leitura liberada a qualquer perfil autenticado (Analista consulta); escrita: Administrador/Gestor.
        var grupo = app.MapGroup("/api/imoveis")
            .WithTags("Imóveis")
            .RequireAuthorization();

        grupo.MapGet("/", async (Guid? clienteId, string? termo, string? tipo, string? status,
            int? skip, int? take, ISender sender, CancellationToken ct) =>
        {
            TipoImovel? tipoImovel = Enum.TryParse<TipoImovel>(tipo, ignoreCase: true, out var t) ? t : null;
            StatusAtivoInativo? statusFiltro = Enum.TryParse<StatusAtivoInativo>(status, ignoreCase: true, out var s) ? s : null;
            var pagina = await sender.Send(
                new ListarImoveisQuery(clienteId, termo, tipoImovel, statusFiltro, skip ?? 0, take ?? 20), ct);
            return Results.Ok(pagina);
        })
        .WithName("ListarImoveis");

        grupo.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var imovel = await sender.Send(new ObterImovelPorIdQuery(id), ct);
            return imovel is null ? Results.NotFound() : Results.Ok(imovel);
        })
        .WithName("ObterImovelPorId");

        grupo.MapPost("/", async (CriarImovelRequest req, ICurrentUser currentUser,
            ISender sender, CancellationToken ct) =>
        {
            // O Gestor só cadastra no próprio cliente; o Administrador informa o cliente alvo.
            var clienteId = currentUser.EhAdministrador ? req.ClienteId : currentUser.ClienteId;
            if (clienteId is null)
                return Results.BadRequest(new { erro = "clienteId é obrigatório para o Administrador." });

            var dto = await sender.Send(new CriarImovelCommand(
                clienteId.Value, req.Nome, req.Tipo, req.NumeroIptu, req.NumeroMatricula, req.Endereco), ct);
            return Results.Created($"/api/imoveis/{dto.Id}", dto);
        })
        .WithName("CriarImovel")
        .RequireAuthorization(Politicas.GerenciaCadastros);

        grupo.MapPut("/{id:guid}", async (Guid id, AtualizarImovelRequest req, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new AtualizarImovelCommand(
                id, req.Nome, req.Tipo, req.NumeroIptu, req.NumeroMatricula, req.Status, req.Endereco), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("AtualizarImovel")
        .RequireAuthorization(Politicas.GerenciaCadastros);

        grupo.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var removido = await sender.Send(new RemoverImovelCommand(id), ct);
            return removido ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoverImovel")
        .RequireAuthorization(Politicas.GerenciaCadastros);

        return app;
    }
}

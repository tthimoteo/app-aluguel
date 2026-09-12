using Aluguel.Application.Abstractions;
using Aluguel.Application.Autorizacao;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Acesso;
using Aluguel.Application.Inquilinos.AtualizarInquilino;
using Aluguel.Application.Inquilinos.CriarInquilino;
using Aluguel.Application.Inquilinos.ListarInquilinos;
using Aluguel.Application.Inquilinos.ObterInquilinoPorId;
using Aluguel.Application.Inquilinos.RemoverInquilino;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Common;
using MediatR;

namespace Aluguel.Api.Endpoints;

public sealed record CriarInquilinoRequest(
    Guid? ClienteId,
    Guid? ImovelId,
    TipoPessoa TipoPessoa,
    string Nome,
    string Documento,
    string? InscricaoMunicipal,
    string? Telefone,
    string? Email,
    EnderecoInput? Endereco);

public sealed record AtualizarInquilinoRequest(
    string Nome,
    string? InscricaoMunicipal,
    string? Telefone,
    string? Email,
    StatusAtivoInativo Status,
    EnderecoInput? Endereco);

public static class InquilinoEndpoints
{
    public static IEndpointRouteBuilder MapInquilinoEndpoints(this IEndpointRouteBuilder app)
    {
        // Leitura liberada a qualquer perfil autenticado (Analista consulta); escrita: Administrador/Gestor.
        var grupo = app.MapGroup("/api/inquilinos")
            .WithTags("Inquilinos")
            .RequireAuthorization();

        grupo.MapGet("/", async (Guid? clienteId, Guid? imovelId, string? termo, string? tipoPessoa, string? status,
            int? skip, int? take, ISender sender, CancellationToken ct) =>
        {
            TipoPessoa? tp = Enum.TryParse<TipoPessoa>(tipoPessoa, ignoreCase: true, out var v) ? v : null;
            StatusAtivoInativo? statusFiltro = Enum.TryParse<StatusAtivoInativo>(status, ignoreCase: true, out var s) ? s : null;
            var pagina = await sender.Send(
                new ListarInquilinosQuery(clienteId, termo, tp, statusFiltro, imovelId, skip ?? 0, take ?? 20), ct);
            return Results.Ok(pagina);
        })
        .WithName("ListarInquilinos");

        grupo.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var inquilino = await sender.Send(new ObterInquilinoPorIdQuery(id), ct);
            return inquilino is null ? Results.NotFound() : Results.Ok(inquilino);
        })
        .WithName("ObterInquilinoPorId");

        grupo.MapPost("/", async (CriarInquilinoRequest req, ICurrentUser currentUser,
            IAuthService auth, ISender sender, CancellationToken ct) =>
        {
            var clienteId = await EscopoVinculo.ExigirClienteComAcessoAsync(
                currentUser, req.ClienteId, auth, ct, exigirGestor: true);

            var dto = await sender.Send(new CriarInquilinoCommand(
                clienteId, req.TipoPessoa, req.Nome, req.Documento,
                req.InscricaoMunicipal, req.Telefone, req.Email, req.Endereco, req.ImovelId), ct);
            return Results.Created($"/api/inquilinos/{dto.Id}", dto);
        })
        .WithName("CriarInquilino")
        .RequireAuthorization(Politicas.GerenciaCadastros);

        grupo.MapPut("/{id:guid}", async (Guid id, AtualizarInquilinoRequest req, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new AtualizarInquilinoCommand(
                id, req.Nome, req.InscricaoMunicipal, req.Telefone, req.Email, req.Status, req.Endereco), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("AtualizarInquilino")
        .RequireAuthorization(Politicas.GerenciaCadastros);

        grupo.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var removido = await sender.Send(new RemoverInquilinoCommand(id), ct);
            return removido ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoverInquilino")
        .RequireAuthorization(Politicas.GerenciaCadastros);

        return app;
    }
}

using Aluguel.Application.Autorizacao;
using Aluguel.Application.Clientes;
using Aluguel.Application.Clientes.AtualizarCliente;
using Aluguel.Application.Clientes.CriarCliente;
using Aluguel.Application.Clientes.ListarClientes;
using Aluguel.Application.Clientes.ObterClientePorId;
using Aluguel.Application.Clientes.RemoverCliente;
using Aluguel.Domain.Clientes;
using MediatR;

namespace Aluguel.Api.Endpoints;

public sealed record CriarClienteRequest(
    TipoPessoa TipoPessoa,
    Guid? PlanoId,
    string? Nome,
    string? Cpf,
    DateOnly? DataNascimento,
    string? RazaoSocial,
    string? NomeFantasia,
    string? Cnpj,
    string? InscricaoMunicipal,
    string? CnaePrincipal,
    string? Telefone,
    string? Email,
    EnderecoInput? Endereco);

public sealed record AtualizarClienteRequest(
    Guid? PlanoId,
    string? Nome,
    DateOnly? DataNascimento,
    string? RazaoSocial,
    string? NomeFantasia,
    string? InscricaoMunicipal,
    string? CnaePrincipal,
    string? Telefone,
    string? Email,
    EnderecoInput? Endereco);

public static class ClienteEndpoints
{
    public static IEndpointRouteBuilder MapClienteEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/clientes")
            .WithTags("Clientes")
            .RequireAuthorization();

        grupo.MapGet("/", async (string? termo, string? tipo, int? skip, int? take, ISender sender, CancellationToken ct) =>
        {
            TipoPessoa? tipoPessoa = Enum.TryParse<TipoPessoa>(tipo, ignoreCase: true, out var t) ? t : null;
            var pagina = await sender.Send(
                new ListarClientesQuery(termo, tipoPessoa, skip ?? 0, take ?? 20), ct);
            return Results.Ok(pagina);
        })
        .WithName("ListarClientes");

        grupo.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var cliente = await sender.Send(new ObterClientePorIdQuery(id), ct);
            return cliente is null ? Results.NotFound() : Results.Ok(cliente);
        })
        .WithName("ObterClientePorId");

        grupo.MapPost("/", async (CriarClienteRequest req, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new CriarClienteCommand(
                req.TipoPessoa, req.PlanoId, req.Nome, req.Cpf, req.DataNascimento,
                req.RazaoSocial, req.NomeFantasia, req.Cnpj, req.InscricaoMunicipal, req.CnaePrincipal,
                req.Telefone, req.Email, req.Endereco), ct);
            return Results.Created($"/api/clientes/{dto.Id}", dto);
        })
        .WithName("CriarCliente")
        .RequireAuthorization(Politicas.GerenciaUsuarios);

        grupo.MapPut("/{id:guid}", async (Guid id, AtualizarClienteRequest req, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new AtualizarClienteCommand(
                id, req.PlanoId, req.Nome, req.DataNascimento, req.RazaoSocial, req.NomeFantasia,
                req.InscricaoMunicipal, req.CnaePrincipal, req.Telefone, req.Email, req.Endereco), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("AtualizarCliente")
        .RequireAuthorization(Politicas.GerenciaUsuarios);

        grupo.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var removido = await sender.Send(new RemoverClienteCommand(id), ct);
            return removido ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoverCliente")
        .RequireAuthorization(Politicas.GerenciaUsuarios);

        return app;
    }
}

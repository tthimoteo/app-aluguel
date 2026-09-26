using Aluguel.Application.Autorizacao;
using Aluguel.Application.Clientes;
using Aluguel.Application.Clientes.AtualizarCliente;
using Aluguel.Application.Clientes.CriarCliente;
using Aluguel.Application.Clientes.EnviarCertificadoCliente;
using Aluguel.Application.Clientes.ListarClientes;
using Aluguel.Application.Clientes.ObterCertificadoCliente;
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
    string? CodigoServico,
    string? RegimeTributario,
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
                req.InscricaoMunicipal, req.CnaePrincipal, req.CodigoServico, req.RegimeTributario,
                req.Telefone, req.Email, req.Endereco), ct);
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

        grupo.MapGet("/{id:guid}/certificado", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new ObterCertificadoClienteQuery(id), ct);
            return dto is null ? Results.NotFound(new { erro = "Nenhum certificado A1 ativo para este cliente." }) : Results.Ok(dto);
        })
        .WithName("ObterCertificadoCliente");

        grupo.MapPost("/{id:guid}/certificado", async (Guid id, HttpRequest http, ISender sender, CancellationToken ct) =>
        {
            if (!http.HasFormContentType)
                return Results.BadRequest(new { erro = "Envie o certificado em multipart/form-data (arquivo + senha)." });

            var form = await http.ReadFormAsync(ct);
            var arquivo = form.Files.GetFile("arquivo") ?? form.Files.FirstOrDefault();
            if (arquivo is null || arquivo.Length == 0)
                return Results.BadRequest(new { erro = "Selecione o arquivo do certificado A1 (.pfx ou .p12)." });

            var senha = form["senha"].ToString();
            await using var stream = arquivo.OpenReadStream();
            try
            {
                var dto = await sender.Send(new EnviarCertificadoClienteCommand(
                    id, stream, arquivo.FileName, arquivo.ContentType ?? "application/x-pkcs12", senha), ct);
                return dto is null ? Results.NotFound() : Results.Ok(dto);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new { erro = ex.Message });
            }
        })
        .WithName("EnviarCertificadoCliente")
        .DisableAntiforgery()
        .RequireAuthorization(Politicas.GerenciaUsuarios);

        return app;
    }
}

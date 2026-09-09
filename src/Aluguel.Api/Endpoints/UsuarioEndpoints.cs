using Aluguel.Application.Abstractions;
using Aluguel.Application.Autorizacao;
using Aluguel.Application.Common.Acesso;
using Aluguel.Application.Usuarios.AtualizarUsuario;
using Aluguel.Application.Usuarios.CriarUsuario;
using Aluguel.Application.Usuarios.ListarUsuarios;
using Aluguel.Application.Usuarios.ObterUsuarioPorCpf;
using Aluguel.Application.Usuarios.ObterUsuarioPorId;
using Aluguel.Application.Usuarios.RemoverUsuario;
using Aluguel.Domain.Usuarios;
using MediatR;

namespace Aluguel.Api.Endpoints;

public sealed record CriarUsuarioRequest(
    Guid? ClienteId,
    string Nome,
    string Email,
    string? Cpf,
    string? Telefone,
    PerfilUsuario Perfil,
    string? Senha);

public sealed record AtualizarUsuarioRequest(
    string Nome,
    string Email,
    string? Telefone,
    PerfilUsuario Perfil,
    StatusUsuario Status);

public static class UsuarioEndpoints
{
    public static IEndpointRouteBuilder MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/usuarios")
            .WithTags("Usuários")
            .RequireAuthorization(Politicas.GerenciaUsuarios);

        grupo.MapGet("/", async (Guid? clienteId, string? termo, int? skip, int? take,
            ISender sender, CancellationToken ct) =>
        {
            var pagina = await sender.Send(
                new ListarUsuariosQuery(clienteId, termo, skip ?? 0, take ?? 20), ct);
            return Results.Ok(pagina);
        })
        .WithName("ListarUsuarios");

        grupo.MapGet("/por-cpf", async (Guid? clienteId, string? cpf, ICurrentUser currentUser,
            IAuthService auth, ISender sender, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return Results.BadRequest(new { erro = "clienteId e cpf são obrigatórios." });

            var cid = await EscopoVinculo.ExigirClienteComAcessoAsync(
                currentUser, clienteId, auth, ct, exigirGestor: true);

            var dto = await sender.Send(new ObterUsuarioPorCpfQuery(cid, cpf), ct);
            return Results.Ok(dto);
        })
        .WithName("ObterUsuarioPorCpf");

        grupo.MapGet("/{id:guid}", async (Guid id, Guid? clienteId, ISender sender, CancellationToken ct) =>
        {
            var usuario = await sender.Send(new ObterUsuarioPorIdQuery(id, clienteId), ct);
            return usuario is null ? Results.NotFound() : Results.Ok(usuario);
        })
        .WithName("ObterUsuarioPorId");

        grupo.MapPost("/", async (CriarUsuarioRequest req, ICurrentUser currentUser,
            IAuthService auth, ISender sender, CancellationToken ct) =>
        {
            var clienteId = await EscopoVinculo.ExigirClienteComAcessoAsync(
                currentUser, req.ClienteId, auth, ct, exigirGestor: true);

            var dto = await sender.Send(new CriarUsuarioCommand(
                clienteId, req.Nome, req.Email, req.Cpf, req.Telefone, req.Perfil, req.Senha), ct);
            return Results.Created($"/api/usuarios/{dto.Id}", dto);
        })
        .WithName("CriarUsuario");

        grupo.MapPut("/{id:guid}", async (Guid id, Guid? clienteId, AtualizarUsuarioRequest req,
            ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new AtualizarUsuarioCommand(
                id, clienteId, req.Nome, req.Email, req.Telefone, req.Perfil, req.Status), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("AtualizarUsuario");

        grupo.MapDelete("/{id:guid}", async (Guid id, Guid? clienteId, ISender sender, CancellationToken ct) =>
        {
            var removido = await sender.Send(new RemoverUsuarioCommand(id, clienteId), ct);
            return removido ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoverUsuario");

        return app;
    }
}

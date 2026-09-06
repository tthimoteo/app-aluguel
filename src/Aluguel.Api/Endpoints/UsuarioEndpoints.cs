using Aluguel.Application.Abstractions;
using Aluguel.Application.Autorizacao;
using Aluguel.Application.Usuarios.AtualizarUsuario;
using Aluguel.Application.Usuarios.CriarUsuario;
using Aluguel.Application.Usuarios.ListarUsuarios;
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
    string Senha);

public sealed record AtualizarUsuarioRequest(
    string Nome,
    string? Telefone,
    PerfilUsuario Perfil,
    StatusUsuario Status);

public static class UsuarioEndpoints
{
    public static IEndpointRouteBuilder MapUsuarioEndpoints(this IEndpointRouteBuilder app)
    {
        // Gestão de usuários do cliente: Administrador (plataforma) ou Gestor (próprio cliente).
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

        grupo.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var usuario = await sender.Send(new ObterUsuarioPorIdQuery(id), ct);
            return usuario is null ? Results.NotFound() : Results.Ok(usuario);
        })
        .WithName("ObterUsuarioPorId");

        grupo.MapPost("/", async (CriarUsuarioRequest req, ICurrentUser currentUser,
            ISender sender, CancellationToken ct) =>
        {
            // O Gestor só cria no próprio cliente; o Administrador informa o cliente alvo.
            var clienteId = currentUser.EhAdministrador ? req.ClienteId : currentUser.ClienteId;
            if (clienteId is null)
                return Results.BadRequest(new { erro = "clienteId é obrigatório para o Administrador." });

            var dto = await sender.Send(new CriarUsuarioCommand(
                clienteId.Value, req.Nome, req.Email, req.Cpf, req.Telefone, req.Perfil, req.Senha), ct);
            return Results.Created($"/api/usuarios/{dto.Id}", dto);
        })
        .WithName("CriarUsuario");

        grupo.MapPut("/{id:guid}", async (Guid id, AtualizarUsuarioRequest req, ISender sender, CancellationToken ct) =>
        {
            var dto = await sender.Send(new AtualizarUsuarioCommand(
                id, req.Nome, req.Telefone, req.Perfil, req.Status), ct);
            return dto is null ? Results.NotFound() : Results.Ok(dto);
        })
        .WithName("AtualizarUsuario");

        grupo.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
        {
            var removido = await sender.Send(new RemoverUsuarioCommand(id), ct);
            return removido ? Results.NoContent() : Results.NotFound();
        })
        .WithName("RemoverUsuario");

        return app;
    }
}

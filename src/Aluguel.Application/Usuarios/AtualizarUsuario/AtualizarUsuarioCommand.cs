using Aluguel.Application.Abstractions;
using Aluguel.Application.Usuarios;
using Aluguel.Domain.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.AtualizarUsuario;

/// <summary>Atualiza dados de um usuário neste cliente. E-mail e CPF são imutáveis.</summary>
public sealed record AtualizarUsuarioCommand(
    Guid Id,
    Guid? ClienteId,
    string Nome,
    string? Telefone,
    PerfilUsuario Perfil,
    StatusUsuario Status) : IRequest<UsuarioDto?>;

public sealed class AtualizarUsuarioCommandHandler(IUsuarioService usuarios, ICurrentUser currentUser)
    : IRequestHandler<AtualizarUsuarioCommand, UsuarioDto?>
{
    public async Task<UsuarioDto?> Handle(AtualizarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var clienteId = currentUser.EhAdministrador ? request.ClienteId : currentUser.ClienteId;
        if (clienteId is null)
            return null;

        var existente = await usuarios.ObterPorIdAsync(request.Id, clienteId, cancellationToken);
        if (existente is null || !AcessoUsuarios.PodeGerenciar(currentUser, existente))
            return null;

        var dados = new AtualizacaoUsuario(request.Nome.Trim(), request.Telefone, request.Perfil, request.Status);
        return await usuarios.AtualizarAsync(request.Id, clienteId.Value, dados, cancellationToken);
    }
}

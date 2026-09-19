using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Application.Usuarios;
using Aluguel.Domain.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.AtualizarUsuario;

/// <summary>Atualiza dados de um usuário neste cliente. O CPF permanece imutável.</summary>
public sealed record AtualizarUsuarioCommand(
    Guid Id,
    Guid? ClienteId,
    string Nome,
    string Email,
    string? Telefone,
    PerfilUsuario Perfil,
    StatusUsuario Status) : IRequest<UsuarioDto?>;

public sealed class AtualizarUsuarioCommandHandler(
    IUsuarioService usuarios,
    ICurrentUser currentUser,
    IAuthService auth)
    : IRequestHandler<AtualizarUsuarioCommand, UsuarioDto?>
{
    public async Task<UsuarioDto?> Handle(AtualizarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var clienteId = await EscopoVinculo.ExigirClienteComAcessoAsync(
            currentUser, request.ClienteId, auth, cancellationToken, exigirGestor: true);

        var existente = await usuarios.ObterPorIdAsync(request.Id, clienteId, cancellationToken);
        if (existente is null || !AcessoUsuarios.PodeGerenciar(currentUser, existente, clienteId))
            return null;

        var dados = new AtualizacaoUsuario(
            request.Nome.Trim(), request.Email.Trim(), request.Telefone, request.Perfil, request.Status);
        return await usuarios.AtualizarAsync(request.Id, clienteId, dados, cancellationToken);
    }
}

using Aluguel.Application.Abstractions;
using Aluguel.Application.Usuarios;
using Aluguel.Domain.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.AtualizarUsuario;

/// <summary>Atualiza dados de um usuário. E-mail e CPF são imutáveis.</summary>
public sealed record AtualizarUsuarioCommand(
    Guid Id,
    string Nome,
    string? Telefone,
    PerfilUsuario Perfil,
    StatusUsuario Status) : IRequest<UsuarioDto?>;

public sealed class AtualizarUsuarioCommandHandler(IUsuarioService usuarios, ICurrentUser currentUser)
    : IRequestHandler<AtualizarUsuarioCommand, UsuarioDto?>
{
    public async Task<UsuarioDto?> Handle(AtualizarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var existente = await usuarios.ObterPorIdAsync(request.Id, cancellationToken);
        if (existente is null || !AcessoUsuarios.PodeGerenciar(currentUser, existente))
            return null;

        var dados = new AtualizacaoUsuario(request.Nome.Trim(), request.Telefone, request.Perfil, request.Status);
        return await usuarios.AtualizarAsync(request.Id, dados, cancellationToken);
    }
}

using Aluguel.Application.Abstractions;
using Aluguel.Application.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.RemoverUsuario;

/// <summary>Desativa (soft delete) um usuário. Retorna false quando não existe/está fora do escopo.</summary>
public sealed record RemoverUsuarioCommand(Guid Id) : IRequest<bool>;

public sealed class RemoverUsuarioCommandHandler(IUsuarioService usuarios, ICurrentUser currentUser)
    : IRequestHandler<RemoverUsuarioCommand, bool>
{
    public async Task<bool> Handle(RemoverUsuarioCommand request, CancellationToken cancellationToken)
    {
        var existente = await usuarios.ObterPorIdAsync(request.Id, cancellationToken);
        if (existente is null || !AcessoUsuarios.PodeGerenciar(currentUser, existente))
            return false;

        if (currentUser.UserId == existente.Id)
            throw new InvalidOperationException("Não é permitido remover o próprio usuário.");

        return await usuarios.RemoverAsync(request.Id, cancellationToken);
    }
}

using Aluguel.Application.Abstractions;
using Aluguel.Application.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.ObterUsuarioPorId;

public sealed record ObterUsuarioPorIdQuery(Guid Id, Guid? ClienteId = null) : IRequest<UsuarioDto?>;

public sealed class ObterUsuarioPorIdQueryHandler(IUsuarioService usuarios, ICurrentUser currentUser)
    : IRequestHandler<ObterUsuarioPorIdQuery, UsuarioDto?>
{
    public async Task<UsuarioDto?> Handle(ObterUsuarioPorIdQuery request, CancellationToken cancellationToken)
    {
        var clienteId = currentUser.EhAdministrador ? request.ClienteId : currentUser.ClienteId;
        var usuario = await usuarios.ObterPorIdAsync(request.Id, clienteId, cancellationToken);
        return usuario is not null && AcessoUsuarios.PodeGerenciar(currentUser, usuario) ? usuario : null;
    }
}

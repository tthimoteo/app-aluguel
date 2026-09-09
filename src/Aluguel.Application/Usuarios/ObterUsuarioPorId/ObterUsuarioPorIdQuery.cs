using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Application.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.ObterUsuarioPorId;

public sealed record ObterUsuarioPorIdQuery(Guid Id, Guid? ClienteId = null) : IRequest<UsuarioDto?>;

public sealed class ObterUsuarioPorIdQueryHandler(
    IUsuarioService usuarios,
    ICurrentUser currentUser,
    IAuthService auth)
    : IRequestHandler<ObterUsuarioPorIdQuery, UsuarioDto?>
{
    public async Task<UsuarioDto?> Handle(ObterUsuarioPorIdQuery request, CancellationToken cancellationToken)
    {
        var clienteId = await EscopoVinculo.ExigirClienteComAcessoAsync(
            currentUser, request.ClienteId, auth, cancellationToken, exigirGestor: true);
        var usuario = await usuarios.ObterPorIdAsync(request.Id, clienteId, cancellationToken);
        return usuario is not null && AcessoUsuarios.PodeGerenciar(currentUser, usuario, clienteId)
            ? usuario
            : null;
    }
}

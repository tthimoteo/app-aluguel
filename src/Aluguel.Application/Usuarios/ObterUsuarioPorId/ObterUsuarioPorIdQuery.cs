using Aluguel.Application.Abstractions;
using Aluguel.Application.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.ObterUsuarioPorId;

public sealed record ObterUsuarioPorIdQuery(Guid Id) : IRequest<UsuarioDto?>;

public sealed class ObterUsuarioPorIdQueryHandler(IUsuarioService usuarios, ICurrentUser currentUser)
    : IRequestHandler<ObterUsuarioPorIdQuery, UsuarioDto?>
{
    public async Task<UsuarioDto?> Handle(ObterUsuarioPorIdQuery request, CancellationToken cancellationToken)
    {
        var usuario = await usuarios.ObterPorIdAsync(request.Id, cancellationToken);
        return usuario is not null && AcessoUsuarios.PodeGerenciar(currentUser, usuario) ? usuario : null;
    }
}

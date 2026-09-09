using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Application.Common.Acesso;
using Aluguel.Application.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.ListarUsuarios;

/// <summary>
/// Lista os usuários de um cliente (filtro por nome/e-mail, paginado). O Gestor lista clientes aos quais
/// está vinculado como Gestor; o Administrador informa <paramref name="ClienteId"/>.
/// </summary>
public sealed record ListarUsuariosQuery(
    Guid? ClienteId = null,
    string? Termo = null,
    int Skip = 0,
    int Take = 20) : IRequest<PaginaDto<UsuarioDto>>;

public sealed class ListarUsuariosQueryHandler(
    IUsuarioService usuarios,
    ICurrentUser currentUser,
    IAuthService auth)
    : IRequestHandler<ListarUsuariosQuery, PaginaDto<UsuarioDto>>
{
    public async Task<PaginaDto<UsuarioDto>> Handle(ListarUsuariosQuery request, CancellationToken cancellationToken)
    {
        var clienteId = await EscopoVinculo.ExigirClienteComAcessoAsync(
            currentUser, request.ClienteId, auth, cancellationToken, exigirGestor: true);

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);

        var total = await usuarios.ContarAsync(clienteId, request.Termo, cancellationToken);
        var itens = await usuarios.ListarAsync(clienteId, request.Termo, skip, take, cancellationToken);

        return new PaginaDto<UsuarioDto>(itens, total, skip, take);
    }
}

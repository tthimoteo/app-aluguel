using Aluguel.Application.Abstractions;
using Aluguel.Application.Clientes;
using Aluguel.Application.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.ListarUsuarios;

/// <summary>
/// Lista os usuários de um cliente (filtro por nome/e-mail, paginado). O Gestor lista apenas o próprio
/// cliente; o Administrador informa <paramref name="ClienteId"/>.
/// </summary>
public sealed record ListarUsuariosQuery(
    Guid? ClienteId = null,
    string? Termo = null,
    int Skip = 0,
    int Take = 20) : IRequest<PaginaDto<UsuarioDto>>;

public sealed class ListarUsuariosQueryHandler(IUsuarioService usuarios, ICurrentUser currentUser)
    : IRequestHandler<ListarUsuariosQuery, PaginaDto<UsuarioDto>>
{
    public async Task<PaginaDto<UsuarioDto>> Handle(ListarUsuariosQuery request, CancellationToken cancellationToken)
    {
        var clienteId = currentUser.EhAdministrador ? request.ClienteId : currentUser.ClienteId;
        if (clienteId is null)
            throw new InvalidOperationException("Informe o clienteId para listar os usuários.");

        var skip = Math.Max(0, request.Skip);
        var take = Math.Clamp(request.Take, 1, 100);

        var total = await usuarios.ContarAsync(clienteId.Value, request.Termo, cancellationToken);
        var itens = await usuarios.ListarAsync(clienteId.Value, request.Termo, skip, take, cancellationToken);

        return new PaginaDto<UsuarioDto>(itens, total, skip, take);
    }
}

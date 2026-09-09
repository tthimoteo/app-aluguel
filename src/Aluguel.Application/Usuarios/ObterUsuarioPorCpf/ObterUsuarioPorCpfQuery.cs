using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Application.Common.Validacoes;
using Aluguel.Application.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.ObterUsuarioPorCpf;

public sealed record ObterUsuarioPorCpfQuery(Guid ClienteId, string Cpf) : IRequest<UsuarioPorCpfDto?>;

public sealed class ObterUsuarioPorCpfQueryHandler(
    IUsuarioService usuarios,
    ICurrentUser currentUser,
    IAuthService auth)
    : IRequestHandler<ObterUsuarioPorCpfQuery, UsuarioPorCpfDto?>
{
    public async Task<UsuarioPorCpfDto?> Handle(ObterUsuarioPorCpfQuery request, CancellationToken cancellationToken)
    {
        var clienteId = await EscopoVinculo.ExigirClienteComAcessoAsync(
            currentUser, request.ClienteId, auth, cancellationToken, exigirGestor: true);

        var cpf = Documento.SomenteDigitos(request.Cpf);
        if (string.IsNullOrEmpty(cpf))
            return null;

        return await usuarios.ObterPorCpfAsync(cpf, clienteId, cancellationToken);
    }
}

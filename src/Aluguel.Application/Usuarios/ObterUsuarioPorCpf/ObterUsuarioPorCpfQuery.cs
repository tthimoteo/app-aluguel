using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Validacoes;
using Aluguel.Application.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.ObterUsuarioPorCpf;

public sealed record ObterUsuarioPorCpfQuery(Guid ClienteId, string Cpf) : IRequest<UsuarioPorCpfDto?>;

public sealed class ObterUsuarioPorCpfQueryHandler(IUsuarioService usuarios, ICurrentUser currentUser)
    : IRequestHandler<ObterUsuarioPorCpfQuery, UsuarioPorCpfDto?>
{
    public Task<UsuarioPorCpfDto?> Handle(ObterUsuarioPorCpfQuery request, CancellationToken cancellationToken)
    {
        var clienteId = currentUser.EhAdministrador ? request.ClienteId : currentUser.ClienteId;
        if (clienteId is null)
            return Task.FromResult<UsuarioPorCpfDto?>(null);

        var cpf = Documento.SomenteDigitos(request.Cpf);
        if (string.IsNullOrEmpty(cpf))
            return Task.FromResult<UsuarioPorCpfDto?>(null);

        return usuarios.ObterPorCpfAsync(cpf, clienteId.Value, cancellationToken);
    }
}

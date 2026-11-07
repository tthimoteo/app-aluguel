using Aluguel.Application.Abstractions;
using Aluguel.Application.Auditoria;
using Aluguel.Application.Common.Acesso;
using Aluguel.Application.Usuarios;
using Aluguel.Domain.Auditoria;
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
    IAuthService auth,
    IAuditoriaService auditoria)
    : IRequestHandler<AtualizarUsuarioCommand, UsuarioDto?>
{
    public async Task<UsuarioDto?> Handle(AtualizarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var clienteId = await EscopoVinculo.ExigirClienteComAcessoAsync(
            currentUser, request.ClienteId, auth, cancellationToken, exigirGestor: true);

        var existente = await usuarios.ObterPorIdAsync(request.Id, clienteId, cancellationToken);
        if (existente is null || !AcessoUsuarios.PodeGerenciar(currentUser, existente, clienteId))
            return null;

        var antes = AuditoriaSnapshots.DeUsuario(existente);
        var dados = new AtualizacaoUsuario(
            request.Nome.Trim(), request.Email.Trim(), request.Telefone, request.Perfil, request.Status);
        var dto = await usuarios.AtualizarAsync(request.Id, clienteId, dados, cancellationToken);
        if (dto is null) return null;

        await auditoria.RegistrarAsync(
            AcaoAuditoria.Alteracao, TabelaAuditoria.Usuario, dto.Id, clienteId,
            antes, AuditoriaSnapshots.DeUsuario(dto),
            resumo: $"Usuário «{dto.Nome}» alterado",
            cancellationToken: cancellationToken);
        return dto;
    }
}

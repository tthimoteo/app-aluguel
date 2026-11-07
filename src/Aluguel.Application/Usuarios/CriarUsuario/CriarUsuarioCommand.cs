using Aluguel.Application.Abstractions;
using Aluguel.Application.Auditoria;
using Aluguel.Application.Common.Validacoes;
using Aluguel.Application.Usuarios;
using Aluguel.Domain.Auditoria;
using Aluguel.Domain.Usuarios;
using MediatR;

namespace Aluguel.Application.Usuarios.CriarUsuario;

/// <summary>Cria um usuário (Gestor/Analista) para um cliente, respeitando o limite do plano (UC002).</summary>
public sealed record CriarUsuarioCommand(
    Guid ClienteId,
    string Nome,
    string Email,
    string? Cpf,
    string? Telefone,
    PerfilUsuario Perfil,
    string? Senha) : IRequest<UsuarioDto>;

public sealed class CriarUsuarioCommandHandler(
    IUsuarioService usuarios,
    ICurrentTenant tenant,
    IAuditoriaService auditoria)
    : IRequestHandler<CriarUsuarioCommand, UsuarioDto>
{
    public async Task<UsuarioDto> Handle(CriarUsuarioCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenant.TenantId
            ?? throw new InvalidOperationException("Tenant não resolvido no token da requisição.");

        var dados = new NovoUsuario(
            tenantId,
            request.ClienteId,
            request.Nome.Trim(),
            request.Email.Trim(),
            Documento.SomenteDigitos(request.Cpf),
            request.Telefone,
            request.Perfil,
            request.Senha);

        var dto = await usuarios.CriarAsync(dados, cancellationToken);
        await auditoria.RegistrarAsync(
            AcaoAuditoria.Cadastro, TabelaAuditoria.Usuario, dto.Id, request.ClienteId,
            null, AuditoriaSnapshots.DeUsuario(dto),
            resumo: $"Usuário «{dto.Nome}» ({dto.Perfil}) cadastrado",
            cancellationToken: cancellationToken);
        return dto;
    }
}

namespace Aluguel.Application.Usuarios;

/// <summary>Representação de leitura de um usuário do cliente (resposta da API).</summary>
public sealed record UsuarioDto(
    Guid Id,
    Guid TenantId,
    Guid? ClienteId,
    string Nome,
    string Email,
    string? Cpf,
    string? Telefone,
    string Perfil,
    string Status,
    DateTimeOffset? UltimoLogin);

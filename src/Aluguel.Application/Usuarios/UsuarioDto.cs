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

/// <summary>Usuário já existente (mesmo CPF) encontrado para vincular a outro cliente.</summary>
public sealed record UsuarioPorCpfDto(
    Guid Id,
    string Nome,
    string Email,
    string? Telefone,
    string Cpf,
    bool JaNoCliente);

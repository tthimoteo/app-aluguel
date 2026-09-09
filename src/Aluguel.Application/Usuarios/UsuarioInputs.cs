using Aluguel.Domain.Usuarios;

namespace Aluguel.Application.Usuarios;

/// <summary>Dados para provisionar um usuário do cliente (CPF já normalizado em dígitos).</summary>
/// <remarks>Senha é obrigatória só na primeira identidade; na vinculação a outro cliente pode ser nula.</remarks>
public sealed record NovoUsuario(
    Guid TenantId,
    Guid ClienteId,
    string Nome,
    string Email,
    string? Cpf,
    string? Telefone,
    PerfilUsuario Perfil,
    string? Senha);

/// <summary>Dados alteráveis de um usuário (e-mail e CPF são imutáveis).</summary>
public sealed record AtualizacaoUsuario(
    string Nome,
    string? Telefone,
    PerfilUsuario Perfil,
    StatusUsuario Status);

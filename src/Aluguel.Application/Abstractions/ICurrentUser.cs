namespace Aluguel.Application.Abstractions;

/// <summary>Usuário autenticado da requisição corrente (resolvido das claims do JWT na API).</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    /// <summary>Cliente ao qual o usuário pertence. Nulo para o Administrador da plataforma.</summary>
    Guid? ClienteId { get; }

    /// <summary>True quando o usuário possui a role Administrador (escopo da plataforma).</summary>
    bool EhAdministrador { get; }
}

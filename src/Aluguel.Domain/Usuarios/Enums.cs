namespace Aluguel.Domain.Usuarios;

/// <summary>Perfil do usuário do cliente (especificação §6) + administrador da plataforma (§4).</summary>
public enum PerfilUsuario
{
    Gestor = 1,
    Analista = 2,
    AdminSistema = 9,
}

/// <summary>Situação do usuário (especificação §6).</summary>
public enum StatusUsuario
{
    Ativo = 1,
    Inativo = 2,
    Bloqueado = 3,
}

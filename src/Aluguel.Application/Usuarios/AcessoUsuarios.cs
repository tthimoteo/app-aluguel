using Aluguel.Application.Abstractions;

namespace Aluguel.Application.Usuarios;

/// <summary>
/// Regra de escopo de acesso aos usuários: o Administrador da plataforma gerencia qualquer cliente;
/// o Gestor gerencia apenas usuários do seu próprio cliente — um usuário nunca vê dados de outro cliente (§6).
/// </summary>
internal static class AcessoUsuarios
{
    public static bool PodeGerenciar(ICurrentUser atual, UsuarioDto alvo) =>
        atual.EhAdministrador || (atual.ClienteId is { } cid && alvo.ClienteId == cid);
}

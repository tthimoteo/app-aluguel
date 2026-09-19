using Aluguel.Application.Abstractions;

namespace Aluguel.Application.Usuarios;

/// <summary>
/// Regra de escopo de acesso aos usuários: o Administrador gerencia qualquer cliente; o Gestor gerencia
/// usuários dos clientes aos quais está vinculado como Gestor — nunca vê dados de cliente sem vínculo (§6).
/// </summary>
internal static class AcessoUsuarios
{
    public static bool PodeGerenciar(ICurrentUser atual, UsuarioDto alvo, Guid clienteEscopo) =>
        atual.EhAdministrador || alvo.ClienteId == clienteEscopo;
}

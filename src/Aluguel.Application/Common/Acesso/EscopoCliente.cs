using Aluguel.Application.Abstractions;

namespace Aluguel.Application.Common.Acesso;

/// <summary>
/// Regras de escopo por cliente para os cadastros operacionais (imóveis, inquilinos e contratos).
/// O Administrador enxerga todo o tenant; os demais perfis ficam restritos ao próprio cliente.
/// </summary>
public static class EscopoCliente
{
    /// <summary>Resolve o cliente alvo de uma operação: o Administrador informa; os demais usam o próprio.</summary>
    public static Guid? Resolver(ICurrentUser atual, Guid? solicitado) =>
        atual.EhAdministrador ? solicitado : atual.ClienteId;

    /// <summary>Indica se o usuário corrente pode acessar/gerenciar registros do cliente informado.</summary>
    public static bool PodeAcessar(ICurrentUser atual, Guid clienteIdAlvo) =>
        atual.EhAdministrador || (atual.ClienteId is { } cid && cid == clienteIdAlvo);
}

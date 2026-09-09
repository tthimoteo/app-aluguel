using Aluguel.Application.Abstractions;

namespace Aluguel.Application.Common.Acesso;

/// <summary>
/// Resolve o cliente de uma operação considerando as vinculações ativas do usuário, não só o
/// <c>cliente_id</c> do JWT. O Administrador informa o cliente; Gestor/Analista acessam qualquer
/// cliente ao qual estejam vinculados.
/// </summary>
public static class EscopoVinculo
{
    /// <summary>
    /// Cliente alvo de listagens operacionais (imóveis, inquilinos, contratos).
    /// Administrador: o informado (nulo = todos). Demais: o informado se houver vínculo; senão o do JWT.
    /// </summary>
    public static async Task<Guid?> ResolverListagemAsync(
        ICurrentUser atual,
        Guid? solicitado,
        IAuthService auth,
        CancellationToken ct)
    {
        if (atual.EhAdministrador)
            return solicitado;

        if (solicitado is null || solicitado == atual.ClienteId)
            return atual.ClienteId;

        await GarantirVinculoAsync(atual, solicitado.Value, auth, ct, exigirGestor: false);
        return solicitado;
    }

    /// <summary>
    /// Cliente obrigatório para gestão de usuários. Administrador informa; Gestor usa o informado
    /// quando tem vínculo de Gestor naquele cliente (ou o cliente do JWT).
    /// </summary>
    public static async Task<Guid> ExigirClienteComAcessoAsync(
        ICurrentUser atual,
        Guid? solicitado,
        IAuthService auth,
        CancellationToken ct,
        bool exigirGestor = true)
    {
        if (atual.EhAdministrador)
        {
            if (solicitado is null)
                throw new InvalidOperationException("Informe o clienteId para listar os usuários.");
            return solicitado.Value;
        }

        var alvo = solicitado ?? atual.ClienteId
            ?? throw new InvalidOperationException("Informe o cliente para continuar.");

        if (alvo == atual.ClienteId)
            return alvo;

        await GarantirVinculoAsync(atual, alvo, auth, ct, exigirGestor);
        return alvo;
    }

    private static async Task GarantirVinculoAsync(
        ICurrentUser atual,
        Guid clienteId,
        IAuthService auth,
        CancellationToken ct,
        bool exigirGestor)
    {
        if (atual.UserId is not { } uid)
            throw new InvalidOperationException("Usuário não autenticado.");

        var vinculos = await auth.ListarVinculosAsync(uid, ct);
        var vinculo = vinculos.FirstOrDefault(v => v.ClienteId == clienteId);
        if (vinculo is null)
            throw new InvalidOperationException("Você não está vinculado a este cliente.");

        if (exigirGestor && !string.Equals(vinculo.Perfil, "Gestor", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Apenas o Gestor deste cliente pode gerenciar usuários.");
    }
}

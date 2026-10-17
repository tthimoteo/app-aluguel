using Aluguel.Application.Abstractions;
using Aluguel.Domain.Assinaturas;

namespace Aluguel.Application.Cobrancas;

/// <summary>Gestor/Analista só acessam Cobrança se o plano do cliente permitir NFS-e (e boletos).</summary>
public static class AcessoCobranca
{
    public static async Task GarantirPlanoPermiteAsync(
        ICurrentUser user,
        IClienteRepository clientes,
        IPlanoRepository planos,
        Guid clienteId,
        CancellationToken ct)
    {
        if (user.EhAdministrador)
            return;

        var cliente = await clientes.ObterPorIdAsync(clienteId, ct)
            ?? throw new InvalidOperationException("Cliente não encontrado.");

        if (cliente.PlanoId is null)
            throw new InvalidOperationException(
                "O módulo Cobrança não está disponível: o cliente não possui plano com emissão de NFS-e e boletos.");

        var plano = await planos.ObterPorIdAsync(cliente.PlanoId.Value, ct);
        if (plano is null || !plano.PermiteModuloCobranca)
            throw new InvalidOperationException(
                "O módulo Cobrança está disponível apenas para planos que permitem emissão de NFS-e e geração de boletos.");
    }

    public static async Task<bool> ClientePermiteAsync(
        IClienteRepository clientes,
        IPlanoRepository planos,
        Guid clienteId,
        CancellationToken ct)
    {
        var cliente = await clientes.ObterPorIdAsync(clienteId, ct);
        if (cliente?.PlanoId is null) return false;
        var plano = await planos.ObterPorIdAsync(cliente.PlanoId.Value, ct);
        return plano?.PermiteModuloCobranca == true;
    }
}

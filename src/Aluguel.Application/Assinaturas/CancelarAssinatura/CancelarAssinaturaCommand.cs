using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Assinaturas;
using MediatR;
using Plano = Aluguel.Domain.Assinaturas.Plano;

namespace Aluguel.Application.Assinaturas.CancelarAssinatura;

public sealed record CancelarAssinaturaCommand(bool Confirmar = true) : IRequest<CancelarAssinaturaResultadoDto>;

public sealed record RevogarCancelamentoAssinaturaCommand : IRequest<AssinaturaAtualDto>;

public sealed class CancelarAssinaturaCommandHandler(
    IAssinaturaRepository assinaturas,
    IPlanoRepository planos,
    ICurrentUser currentUser,
    ICurrentTenant tenant,
    TimeProvider clock)
    : IRequestHandler<CancelarAssinaturaCommand, CancelarAssinaturaResultadoDto>
{
    public async Task<CancelarAssinaturaResultadoDto> Handle(
        CancelarAssinaturaCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.ClienteId is not { } clienteId)
            throw new InvalidOperationException("Somente gestores do cliente podem cancelar a assinatura.");

        if (!EscopoCliente.PodeAcessar(currentUser, clienteId))
            throw new InvalidOperationException("Sem permissão para cancelar esta assinatura.");

        var assinatura = await assinaturas.ObterVigentePorClienteAsync(clienteId, cancellationToken)
            ?? throw new InvalidOperationException("Nenhuma assinatura vigente encontrada.");

        var agora = clock.GetUtcNow();
        assinatura.SolicitarCancelamento(agora);

        var plano = await planos.ObterPorIdAsync(assinatura.PlanoId, cancellationToken);
        var tenantId = tenant.TenantId ?? assinatura.TenantId;

        assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
            tenantId, clienteId, EventoAssinatura.Cancelamento,
            assinaturaId: assinatura.Id,
            usuarioId: currentUser.UserId,
            planoAnterior: plano?.Codigo,
            novoPlano: plano?.Codigo,
            valor: assinatura.ValorMensal,
            descricao:
            $"Cancelamento solicitado — acesso até {assinatura.FimCicloPago:yyyy-MM-dd}; " +
            "depois o login será bloqueado. Sem novas cobranças."));

        await assinaturas.SalvarAlteracoesAsync(cancellationToken);

        return new CancelarAssinaturaResultadoDto(
            assinatura.Id,
            assinatura.Status.ToString(),
            assinatura.FimCicloPago,
            assinatura.CancelamentoSolicitadoEm!.Value,
            $"Cancelamento registrado. Você mantém acesso até " +
            $"{assinatura.FimCicloPago:dd/MM/yyyy}. Após essa data o login será bloqueado.");
    }
}

public sealed class RevogarCancelamentoAssinaturaCommandHandler(
    IAssinaturaRepository assinaturas,
    IPlanoRepository planos,
    ICurrentUser currentUser,
    ICurrentTenant tenant)
    : IRequestHandler<RevogarCancelamentoAssinaturaCommand, AssinaturaAtualDto>
{
    public async Task<AssinaturaAtualDto> Handle(
        RevogarCancelamentoAssinaturaCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.ClienteId is not { } clienteId)
            throw new InvalidOperationException("Somente gestores do cliente podem revogar o cancelamento.");

        var assinatura = await assinaturas.ObterVigentePorClienteAsync(clienteId, cancellationToken)
            ?? throw new InvalidOperationException("Nenhuma assinatura vigente encontrada.");

        assinatura.RevogarCancelamento();

        var plano = await planos.ObterPorIdAsync(assinatura.PlanoId, cancellationToken);
        var tenantId = tenant.TenantId ?? assinatura.TenantId;
        assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
            tenantId, clienteId, EventoAssinatura.Reativacao,
            assinaturaId: assinatura.Id,
            usuarioId: currentUser.UserId,
            novoPlano: plano?.Codigo,
            descricao: "Cancelamento revogado — renovação mensal permanece ativa"));

        await assinaturas.SalvarAlteracoesAsync(cancellationToken);

        Plano? agendado = null;
        if (assinatura.PlanoAgendadoId is { } aid)
            agendado = await planos.ObterPorIdAsync(aid, cancellationToken);

        return new AssinaturaAtualDto(
            assinatura.Id,
            assinatura.ClienteId,
            assinatura.PlanoId,
            plano?.Nome ?? "Plano",
            plano?.Codigo ?? "",
            assinatura.Status.ToString(),
            assinatura.ValorMensal,
            assinatura.ProximaCobranca,
            assinatura.FimCicloPago,
            assinatura.PlanoAgendadoId,
            agendado?.Nome,
            assinatura.CancelamentoSolicitadoEm,
            assinatura.CancelamentoPendente,
            assinatura.DowngradeAgendado);
    }
}

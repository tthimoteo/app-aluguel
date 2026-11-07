using Aluguel.Application.Abstractions;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Clientes;
using MediatR;

namespace Aluguel.Application.Assinaturas.ProcessarCicloAssinaturas;

/// <summary>
/// Processa o ciclo mensal: efetiva cancelamentos vencidos, aplica downgrades agendados
/// e gera cobrança PIX de renovação.
/// </summary>
public sealed record ProcessarCicloAssinaturasCommand(bool ForcarTodas = false)
    : IRequest<ProcessarCicloAssinaturasResultado>;

public sealed record ProcessarCicloAssinaturasResultado(
    int CancelamentosEfetivados,
    int RenovacoesGeradas,
    int DowngradesAplicados,
    IReadOnlyList<string> Mensagens);

public sealed class ProcessarCicloAssinaturasCommandHandler(
    IAssinaturaRepository assinaturas,
    IClienteRepository clientes,
    IPlanoRepository planos,
    IInterPixProvider pix,
    TimeProvider clock)
    : IRequestHandler<ProcessarCicloAssinaturasCommand, ProcessarCicloAssinaturasResultado>
{
    private static readonly TimeSpan ToleranciaInadimplencia = TimeSpan.FromDays(7);

    public async Task<ProcessarCicloAssinaturasResultado> Handle(
        ProcessarCicloAssinaturasCommand request, CancellationToken cancellationToken)
    {
        var agora = clock.GetUtcNow();
        var msgs = new List<string>();
        var cancelados = 0;
        var renovacoes = 0;
        var downgrades = 0;

        var aVencer = await assinaturas.ListarParaProcessamentoCicloAsync(agora, request.ForcarTodas, cancellationToken);

        foreach (var assinatura in aVencer)
        {
            try
            {
                if (assinatura.CancelamentoSolicitadoEm is not null
                    && assinatura.Status == StatusAssinatura.Ativa
                    && (request.ForcarTodas
                        || assinatura.FimCicloPago is null
                        || assinatura.FimCicloPago <= agora))
                {
                    await EfetivarCancelamentoAsync(assinatura, agora, msgs, cancellationToken);
                    cancelados++;
                    continue;
                }

                if (assinatura.Status == StatusAssinatura.PendentePagamento
                    && assinatura.InicioTolerancia is { } inicio
                    && agora - inicio > ToleranciaInadimplencia)
                {
                    assinatura.SuspenderPorInadimplencia(agora);
                    assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
                        assinatura.TenantId, assinatura.ClienteId, EventoAssinatura.Suspensao,
                        assinaturaId: assinatura.Id,
                        descricao: "Suspensa por inadimplência após tolerância de 7 dias"));
                    await assinaturas.SalvarAlteracoesAsync(cancellationToken);
                    msgs.Add($"Assinatura {assinatura.Id}: suspensa por inadimplência");
                    continue;
                }

                if (assinatura.Status != StatusAssinatura.Ativa)
                    continue;

                if (assinatura.CancelamentoSolicitadoEm is not null)
                    continue; // aguarda fim do ciclo

                if (!request.ForcarTodas
                    && assinatura.ProximaCobranca is { } prox
                    && prox > agora)
                    continue;

                var aplicouDowngrade = await AplicarDowngradeSeHouverAsync(
                    assinatura, agora, msgs, cancellationToken);
                if (aplicouDowngrade) downgrades++;

                await GerarRenovacaoPixAsync(assinatura, agora, msgs, cancellationToken);
                renovacoes++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                msgs.Add($"Assinatura {assinatura.Id}: erro — {ex.Message}");
            }
        }

        return new ProcessarCicloAssinaturasResultado(cancelados, renovacoes, downgrades, msgs);
    }

    private async Task EfetivarCancelamentoAsync(
        Assinatura assinatura, DateTimeOffset agora, List<string> msgs, CancellationToken ct)
    {
        var cliente = await clientes.ObterPorIdAsync(assinatura.ClienteId, ct)
            ?? throw new InvalidOperationException("Cliente não encontrado.");

        assinatura.EfetivarCancelamentoAoFimDoCiclo(agora);
        cliente.DefinirStatus(StatusCliente.Bloqueado);

        assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
            assinatura.TenantId, cliente.Id, EventoAssinatura.Cancelamento,
            assinaturaId: assinatura.Id,
            descricao: "Fim do período pago — assinatura cancelada e login bloqueado"));

        await assinaturas.SalvarAlteracoesAsync(ct);
        msgs.Add($"Assinatura {assinatura.Id}: cancelada; cliente bloqueado");
    }

    private async Task<bool> AplicarDowngradeSeHouverAsync(
        Assinatura assinatura, DateTimeOffset agora, List<string> msgs, CancellationToken ct)
    {
        if (assinatura.PlanoAgendadoId is not { } planoAgendadoId)
            return false;

        var plano = await planos.ObterPorIdAsync(planoAgendadoId, ct)
            ?? throw new InvalidOperationException("Plano agendado não encontrado.");

        if (plano.ValorMensal is null or <= 0)
            throw new InvalidOperationException($"Plano {plano.Nome} sem valor mensal para renovação.");

        var planoAnterior = await planos.ObterPorIdAsync(assinatura.PlanoId, ct);
        var cliente = await clientes.ObterPorIdAsync(assinatura.ClienteId, ct)
            ?? throw new InvalidOperationException("Cliente não encontrado.");

        assinatura.AplicarDowngradeAgendadoSeHouver(plano.Id, plano.ValorMensal.Value);
        cliente.DefinirPlano(plano.Id);

        assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
            assinatura.TenantId, cliente.Id, EventoAssinatura.Downgrade,
            assinaturaId: assinatura.Id,
            planoAnterior: planoAnterior?.Codigo,
            novoPlano: plano.Codigo,
            valor: plano.ValorMensal,
            descricao: $"Downgrade efetivado na renovação para {plano.Nome} — sem estorno/crédito"));

        await assinaturas.SalvarAlteracoesAsync(ct);
        msgs.Add($"Assinatura {assinatura.Id}: downgrade para {plano.Nome}");
        return true;
    }

    private async Task GerarRenovacaoPixAsync(
        Assinatura assinatura, DateTimeOffset agora, List<string> msgs, CancellationToken ct)
    {
        var plano = await planos.ObterPorIdAsync(assinatura.PlanoId, ct)
            ?? throw new InvalidOperationException("Plano da assinatura não encontrado.");

        if (plano.ValorMensal is null or <= 0)
            throw new InvalidOperationException($"Plano {plano.Nome} sem valor para renovação.");

        assinatura.IniciarRenovacaoAguardandoPagamento(agora);

        var (pagamento, _) = await AssinaturaPixHelper.CriarCobrancaPixAsync(
            assinatura,
            plano.Id,
            plano.ValorMensal.Value,
            $"Renovação Lucrare — plano {plano.Nome}",
            assinaturaJaRastreada: true,
            assinaturas,
            pix,
            agora,
            ct);

        assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
            assinatura.TenantId, assinatura.ClienteId, EventoAssinatura.Renovacao,
            assinaturaId: assinatura.Id,
            novoPlano: plano.Codigo,
            valor: plano.ValorMensal,
            descricao: $"Renovação: PIX gerado (txid {pagamento.PixTxId})"));

        await assinaturas.SalvarAlteracoesAsync(ct);
        msgs.Add($"Assinatura {assinatura.Id}: renovação PIX {pagamento.Id}");
    }
}

using System.Text.Json;
using Aluguel.Application.Abstractions;
using Aluguel.Domain.Assinaturas;
using MediatR;

namespace Aluguel.Application.Assinaturas.ProcessarWebhookPixInter;

/// <summary>
/// Processa notificação de liquidação PIX do Banco Inter e ativa a assinatura.
/// Idempotente por endToEndId / status já pago.
/// </summary>
public sealed record ProcessarWebhookPixInterCommand(string PayloadJson)
    : IRequest<ProcessarWebhookPixInterResultado>;

public sealed record ProcessarWebhookPixInterResultado(int Processados, int Ignorados, IReadOnlyList<string> Mensagens);

public sealed class ProcessarWebhookPixInterCommandHandler(
    IAssinaturaRepository assinaturas,
    IClienteRepository clientes,
    IPlanoRepository planos,
    TimeProvider clock)
    : IRequestHandler<ProcessarWebhookPixInterCommand, ProcessarWebhookPixInterResultado>
{
    public async Task<ProcessarWebhookPixInterResultado> Handle(
        ProcessarWebhookPixInterCommand request, CancellationToken cancellationToken)
    {
        var eventos = ExtrairEventos(request.PayloadJson);
        var processados = 0;
        var ignorados = 0;
        var msgs = new List<string>();

        foreach (var ev in eventos)
        {
            if (string.IsNullOrWhiteSpace(ev.TxId))
            {
                ignorados++;
                msgs.Add("Evento sem txid ignorado");
                continue;
            }

            var pagamento = await assinaturas.ObterPagamentoPorTxIdAsync(ev.TxId, cancellationToken);
            if (pagamento is null)
            {
                ignorados++;
                msgs.Add($"txid {ev.TxId} não encontrado");
                continue;
            }

            if (pagamento.Status == StatusPagamentoPlano.Pago)
            {
                ignorados++;
                msgs.Add($"txid {ev.TxId} já liquidado");
                continue;
            }

            var assinatura = await assinaturas.ObterPorPagamentoIdAsync(pagamento.Id, cancellationToken)
                ?? throw new InvalidOperationException($"Assinatura do pagamento {pagamento.Id} não encontrada.");

            // Usar a instância rastreada na coleção da assinatura (mesmo DbContext).
            var pagamentoTracked = assinatura.Pagamentos.FirstOrDefault(p => p.Id == pagamento.Id)
                ?? pagamento;

            var cliente = await clientes.ObterPorIdAsync(assinatura.ClienteId, cancellationToken)
                ?? throw new InvalidOperationException("Cliente da assinatura não encontrado.");

            var plano = await planos.ObterPorIdAsync(pagamentoTracked.PlanoDestinoId, cancellationToken)
                ?? throw new InvalidOperationException("Plano destino do pagamento não encontrado.");

            var agora = clock.GetUtcNow();
            var endToEnd = string.IsNullOrWhiteSpace(ev.EndToEndId)
                ? $"E{ev.TxId}"
                : ev.EndToEndId!;

            pagamentoTracked.ConfirmarPagamentoPix(endToEnd, agora);
            // Reinicia o ciclo mensal com o plano/valor pagos (upgrade ou renovação).
            assinatura.AtivarAposPagamento(agora, plano.Id, pagamentoTracked.Valor);
            cliente.DefinirPlano(plano.Id);
            if (cliente.Status != Domain.Clientes.StatusCliente.Ativo)
                cliente.DefinirStatus(Domain.Clientes.StatusCliente.Ativo);

            assinaturas.AdicionarAuditoria(new AuditoriaAssinatura(
                assinatura.TenantId,
                cliente.Id,
                EventoAssinatura.Pagamento,
                assinaturaId: assinatura.Id,
                novoPlano: plano.Codigo,
                valor: pagamentoTracked.Valor,
                descricao:
                $"PIX liquidado endToEndId={endToEnd} — plano {plano.Nome}; " +
                $"próxima cobrança {assinatura.ProximaCobranca:yyyy-MM-dd}"));

            await assinaturas.SalvarAlteracoesAsync(cancellationToken);

            processados++;
            msgs.Add($"txid {ev.TxId} liquidado — plano {plano.Nome} ativado");
        }

        return new ProcessarWebhookPixInterResultado(processados, ignorados, msgs);
    }

    public static IReadOnlyList<PixLiquidacaoEvento> ExtrairEventos(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return [];

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var lista = new List<PixLiquidacaoEvento>();

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
                ColetarPix(item, lista);
        }
        else
        {
            ColetarPix(root, lista);
        }

        return lista;
    }

    private static void ColetarPix(JsonElement el, List<PixLiquidacaoEvento> dest)
    {
        if (el.TryGetProperty("pix", out var pixArr) && pixArr.ValueKind == JsonValueKind.Array)
        {
            foreach (var p in pixArr.EnumerateArray())
                dest.Add(LerPix(p));
            return;
        }

        if (el.TryGetProperty("txid", out _) || el.TryGetProperty("txId", out _))
            dest.Add(LerPix(el));
    }

    private static PixLiquidacaoEvento LerPix(JsonElement p)
    {
        string? Get(params string[] names)
        {
            foreach (var n in names)
            {
                if (p.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.String)
                    return v.GetString();
            }
            return null;
        }

        return new PixLiquidacaoEvento(
            TxId: Get("txid", "txId"),
            EndToEndId: Get("endToEndId", "end_to_end_id"),
            Valor: Get("valor"),
            Horario: Get("horario"));
    }
}

public sealed record PixLiquidacaoEvento(string? TxId, string? EndToEndId, string? Valor, string? Horario);

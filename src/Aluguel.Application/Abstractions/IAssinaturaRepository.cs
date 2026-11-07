using Aluguel.Domain.Assinaturas;

namespace Aluguel.Application.Abstractions;

/// <summary>Porta de acesso ao agregado Assinatura e pagamentos de plano.</summary>
public interface IAssinaturaRepository
{
    Task<Assinatura?> ObterVigentePorClienteAsync(Guid clienteId, CancellationToken ct = default);

    Task<Assinatura?> ObterPorIdAsync(Guid id, CancellationToken ct = default);

    Task<PagamentoPlano?> ObterPagamentoPorIdAsync(Guid pagamentoId, CancellationToken ct = default);

    Task<PagamentoPlano?> ObterPagamentoPorTxIdAsync(string pixTxId, CancellationToken ct = default);

    Task<Assinatura?> ObterPorPagamentoIdAsync(Guid pagamentoId, CancellationToken ct = default);

    /// <summary>
    /// Assinaturas candidatas a renovação, efetivaçao de cancelamento ou suspensão por inadimplência.
    /// </summary>
    Task<IReadOnlyList<Assinatura>> ListarParaProcessamentoCicloAsync(
        DateTimeOffset agora, bool forcarTodas, CancellationToken ct = default);

    void Adicionar(Assinatura assinatura);

    void AdicionarPagamento(PagamentoPlano pagamento);

    void AdicionarAuditoria(AuditoriaAssinatura auditoria);

    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
}

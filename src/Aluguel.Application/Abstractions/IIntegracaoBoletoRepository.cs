using Aluguel.Domain.Financeiro;

namespace Aluguel.Application.Abstractions;

public interface IIntegracaoBoletoRepository
{
    Task<IntegracaoBoletoCliente?> ObterAtivaPorClienteAsync(Guid clienteId, CancellationToken ct = default);
    void Adicionar(IntegracaoBoletoCliente integracao);
    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
}

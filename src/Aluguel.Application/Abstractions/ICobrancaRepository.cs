using Aluguel.Application.Cobrancas;
using Aluguel.Domain.Fiscal;

namespace Aluguel.Application.Abstractions;

/// <summary>Consultas do histórico de cobrança (NFS-e + boleto + pagamento).</summary>
public interface ICobrancaRepository
{
    Task<(IReadOnlyList<CobrancaListItemDto> Itens, int Total)> ListarAsync(
        Guid? clienteId, Guid? imovelId, string? competencia,
        int skip, int take, CancellationToken ct = default);

    Task<CobrancaDetalheDto?> ObterDetalheAsync(Guid id, CancellationToken ct = default);

    Task<NotaFiscalServico?> ObterNfseAsync(Guid id, CancellationToken ct = default);

    Task<Domain.Financeiro.Boleto?> ObterBoletoPorNfseAsync(Guid nfseId, CancellationToken ct = default);

    Task<DocumentoFiscal?> ObterDocumentoAsync(Guid nfseId, TipoDocumentoFiscal tipo, CancellationToken ct = default);

    /// <summary>CASO 6/7/8: já existe faturamento não cancelado para o imóvel/competência?</summary>
    Task<bool> ExisteCobrancaNaoCanceladaAsync(Guid imovelId, string competencia, CancellationToken ct = default);

    void AdicionarNfse(NotaFiscalServico nfse);
    void AdicionarDocumento(DocumentoFiscal documento);
    void AdicionarBoleto(Domain.Financeiro.Boleto boleto);
    void AdicionarPagamento(Domain.Financeiro.Pagamento pagamento);
    Task<int> SalvarAlteracoesAsync(CancellationToken ct = default);
    Task<int> ContarNfseAsync(CancellationToken ct = default);
}

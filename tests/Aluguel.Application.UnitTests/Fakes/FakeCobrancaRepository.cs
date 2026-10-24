using Aluguel.Application.Abstractions;
using Aluguel.Application.Cobrancas;
using Aluguel.Domain.Financeiro;
using Aluguel.Domain.Fiscal;

namespace Aluguel.Application.UnitTests.Fakes;

public sealed class FakeCobrancaRepository : ICobrancaRepository
{
    public List<NotaFiscalServico> Nfses { get; } = [];
    public List<Boleto> Boletos { get; } = [];
    public List<Pagamento> Pagamentos { get; } = [];
    public List<DocumentoFiscal> Documentos { get; } = [];
    public int SalvouVezes { get; private set; }

    public Task<(IReadOnlyList<CobrancaListItemDto> Itens, int Total)> ListarAsync(
        Guid? clienteId, Guid? imovelId, string? competencia,
        int skip, int take, CancellationToken ct = default) =>
        Task.FromResult<(IReadOnlyList<CobrancaListItemDto>, int)>(([], 0));

    public Task<CobrancaResumoDto> ContarResumoVencimentosAsync(
        Guid? clienteId, DateOnly hoje, CancellationToken ct = default)
    {
        var abertas = Boletos
            .Where(b => b.NfseId is { } nid
                        && Nfses.Any(n => n.Id == nid
                                         && n.Status != StatusNfse.Cancelada
                                         && (clienteId is null || n.ClienteId == clienteId))
                        && Pagamentos.All(p => p.NfseId != nid))
            .Select(b => b.DataVencimento)
            .ToList();
        return Task.FromResult(new CobrancaResumoDto(
            abertas.Count,
            abertas.Count(d => d < hoje)));
    }

    public Task<CobrancaDetalheDto?> ObterDetalheAsync(Guid id, CancellationToken ct = default)
    {
        var nfse = Nfses.FirstOrDefault(n => n.Id == id);
        if (nfse is null) return Task.FromResult<CobrancaDetalheDto?>(null);

        var pag = Pagamentos.FirstOrDefault(p => p.NfseId == id);
        var boleto = Boletos.FirstOrDefault(b => b.NfseId == id);

        return Task.FromResult<CobrancaDetalheDto?>(new CobrancaDetalheDto(
            nfse.Id,
            nfse.ClienteId,
            "Cliente",
            nfse.ImovelId,
            "Imóvel",
            nfse.InquilinoId,
            null,
            nfse.Competencia,
            nfse.Numero,
            nfse.Serie,
            nfse.ChaveAcesso,
            nfse.Status.ToString(),
            nfse.ValorServico,
            nfse.Desconto,
            nfse.Multa,
            nfse.Juros,
            nfse.ValorFaturado,
            nfse.DataEmissao,
            pag is not null,
            pag?.DataPagamento,
            pag?.ValorPago,
            Documentos.Any(d => d.NfseId == id && d.Tipo == TipoDocumentoFiscal.XML),
            Documentos.Any(d => d.NfseId == id && d.Tipo == TipoDocumentoFiscal.PDF),
            boleto?.PossuiPdf == true,
            boleto?.Id,
            boleto?.NossoNumero,
            boleto?.LinhaDigitavel,
            boleto?.DataVencimento,
            boleto?.Valor,
            boleto?.Status.ToString()));
    }

    public Task<NotaFiscalServico?> ObterNfseAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Nfses.FirstOrDefault(n => n.Id == id));

    public Task<Boleto?> ObterBoletoPorNfseAsync(Guid nfseId, CancellationToken ct = default) =>
        Task.FromResult(Boletos.FirstOrDefault(b => b.NfseId == nfseId));

    public Task<DocumentoFiscal?> ObterDocumentoAsync(Guid nfseId, TipoDocumentoFiscal tipo, CancellationToken ct = default) =>
        Task.FromResult(Documentos.FirstOrDefault(d => d.NfseId == nfseId && d.Tipo == tipo));

    public Task<Pagamento?> ObterPagamentoPorNfseAsync(Guid nfseId, CancellationToken ct = default) =>
        Task.FromResult(Pagamentos.FirstOrDefault(p => p.NfseId == nfseId));

    public Task<bool> ExisteCobrancaNaoCanceladaAsync(Guid imovelId, string competencia, CancellationToken ct = default) =>
        Task.FromResult(Nfses.Any(n => n.ImovelId == imovelId
            && n.Competencia == competencia
            && n.Status != StatusNfse.Cancelada));

    public void AdicionarNfse(NotaFiscalServico nfse) => Nfses.Add(nfse);
    public void AdicionarDocumento(DocumentoFiscal documento) => Documentos.Add(documento);
    public void AdicionarBoleto(Boleto boleto) => Boletos.Add(boleto);
    public void AdicionarPagamento(Pagamento pagamento) => Pagamentos.Add(pagamento);

    public Task RemoverCobrancaAsync(Guid nfseId, CancellationToken ct = default)
    {
        Documentos.RemoveAll(d => d.NfseId == nfseId);
        Boletos.RemoveAll(b => b.NfseId == nfseId);
        Nfses.RemoveAll(n => n.Id == nfseId);
        return Task.CompletedTask;
    }

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default)
    {
        SalvouVezes++;
        return Task.FromResult(1);
    }

    public Task<int> ContarNfseAsync(CancellationToken ct = default) =>
        Task.FromResult(Nfses.Count);
}

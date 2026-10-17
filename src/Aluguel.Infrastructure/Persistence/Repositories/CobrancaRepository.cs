using Aluguel.Application.Abstractions;
using Aluguel.Application.Cobrancas;
using Aluguel.Domain.Financeiro;
using Aluguel.Domain.Fiscal;
using Microsoft.EntityFrameworkCore;

namespace Aluguel.Infrastructure.Persistence.Repositories;

public class CobrancaRepository(AppDbContext db) : ICobrancaRepository
{
    public async Task<(IReadOnlyList<CobrancaListItemDto> Itens, int Total)> ListarAsync(
        Guid? clienteId, Guid? imovelId, string? competencia,
        int skip, int take, CancellationToken ct = default)
    {
        var query = db.NotasFiscais.AsNoTracking().AsQueryable();

        if (clienteId is { } cid)
            query = query.Where(n => n.ClienteId == cid);
        if (imovelId is { } iid)
            query = query.Where(n => n.ImovelId == iid);
        if (!string.IsNullOrWhiteSpace(competencia))
            query = query.Where(n => n.Competencia == competencia.Trim());

        var total = await query.CountAsync(ct);

        var pagina = await query
            .OrderByDescending(n => n.Competencia)
            .ThenByDescending(n => n.CreatedAt)
            .Skip(skip)
            .Take(take)
            .Select(n => new
            {
                n.Id,
                n.ClienteId,
                n.ImovelId,
                n.Competencia,
                n.ValorFaturado,
                Status = n.Status.ToString(),
                TemXml = n.Documentos.Any(d => d.Tipo == TipoDocumentoFiscal.XML),
                TemPdf = n.Documentos.Any(d => d.Tipo == TipoDocumentoFiscal.PDF),
            })
            .ToListAsync(ct);

        var ids = pagina.Select(p => p.Id).ToList();
        var clienteIds = pagina.Select(p => p.ClienteId).Distinct().ToList();
        var imovelIds = pagina.Select(p => p.ImovelId).Distinct().ToList();

        var clientes = await db.Clientes.AsNoTracking()
            .Where(c => clienteIds.Contains(c.Id))
            .Select(c => new { c.Id, Nome = c.Nome ?? c.RazaoSocial ?? "" })
            .ToDictionaryAsync(c => c.Id, c => c.Nome, ct);

        var imoveis = await db.Imoveis.AsNoTracking()
            .Where(i => imovelIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Nome })
            .ToDictionaryAsync(i => i.Id, i => i.Nome, ct);

        var pagamentos = await db.Pagamentos.AsNoTracking()
            .Where(p => p.NfseId != null && ids.Contains(p.NfseId.Value))
            .Select(p => new { NfseId = p.NfseId!.Value, p.DataPagamento })
            .ToDictionaryAsync(p => p.NfseId, p => (DateOnly?)p.DataPagamento, ct);

        var boletos = await db.Boletos.AsNoTracking()
            .Where(b => b.NfseId != null && ids.Contains(b.NfseId.Value))
            .Select(b => new { NfseId = b.NfseId!.Value, TemPdf = b.StoragePathPdf != null && b.StoragePathPdf != "" })
            .ToDictionaryAsync(b => b.NfseId, b => b.TemPdf, ct);

        var itens = pagina.Select(p =>
        {
            DateOnly? dataPagamento = pagamentos.TryGetValue(p.Id, out var dp) ? dp : null;
            return new CobrancaListItemDto(
                p.Id,
                p.ClienteId,
                clientes.GetValueOrDefault(p.ClienteId, "—"),
                p.ImovelId,
                imoveis.GetValueOrDefault(p.ImovelId, "—"),
                p.Competencia,
                p.ValorFaturado,
                p.Status,
                dataPagamento is not null,
                dataPagamento,
                p.TemXml,
                p.TemPdf,
                boletos.GetValueOrDefault(p.Id));
        }).ToList();

        return (itens, total);
    }

    public async Task<CobrancaDetalheDto?> ObterDetalheAsync(Guid id, CancellationToken ct = default)
    {
        var n = await db.NotasFiscais.AsNoTracking()
            .Include(x => x.Documentos)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
        if (n is null) return null;

        var clienteNome = await db.Clientes.AsNoTracking()
            .Where(c => c.Id == n.ClienteId)
            .Select(c => c.Nome ?? c.RazaoSocial ?? "")
            .FirstOrDefaultAsync(ct) ?? "—";

        var imovelNome = await db.Imoveis.AsNoTracking()
            .Where(i => i.Id == n.ImovelId)
            .Select(i => i.Nome)
            .FirstOrDefaultAsync(ct) ?? "—";

        string? inquilinoNome = null;
        if (n.InquilinoId is { } inqId)
        {
            inquilinoNome = await db.Inquilinos.AsNoTracking()
                .Where(i => i.Id == inqId)
                .Select(i => i.Nome)
                .FirstOrDefaultAsync(ct);
        }

        var pagamento = await db.Pagamentos.AsNoTracking()
            .FirstOrDefaultAsync(p => p.NfseId == n.Id, ct);

        var boleto = await db.Boletos.AsNoTracking()
            .FirstOrDefaultAsync(b => b.NfseId == n.Id, ct);

        return new CobrancaDetalheDto(
            n.Id,
            n.ClienteId,
            clienteNome,
            n.ImovelId,
            imovelNome,
            n.InquilinoId,
            inquilinoNome,
            n.Competencia,
            n.Numero,
            n.Serie,
            n.ChaveAcesso,
            n.Status.ToString(),
            n.ValorServico,
            n.Desconto,
            n.Multa,
            n.Juros,
            n.ValorFaturado,
            n.DataEmissao,
            pagamento is not null,
            pagamento?.DataPagamento,
            pagamento?.ValorPago,
            n.Documentos.Any(d => d.Tipo == TipoDocumentoFiscal.XML),
            n.Documentos.Any(d => d.Tipo == TipoDocumentoFiscal.PDF),
            boleto?.PossuiPdf == true,
            boleto?.Id,
            boleto?.NossoNumero,
            boleto?.LinhaDigitavel,
            boleto?.DataVencimento,
            boleto?.Valor,
            boleto?.Status.ToString());
    }

    public Task<NotaFiscalServico?> ObterNfseAsync(Guid id, CancellationToken ct = default) =>
        db.NotasFiscais.Include(n => n.Documentos).FirstOrDefaultAsync(n => n.Id == id, ct);

    public Task<Boleto?> ObterBoletoPorNfseAsync(Guid nfseId, CancellationToken ct = default) =>
        db.Boletos.FirstOrDefaultAsync(b => b.NfseId == nfseId, ct);

    public Task<DocumentoFiscal?> ObterDocumentoAsync(Guid nfseId, TipoDocumentoFiscal tipo, CancellationToken ct = default) =>
        db.DocumentosFiscais.FirstOrDefaultAsync(d => d.NfseId == nfseId && d.Tipo == tipo, ct);

    public void AdicionarNfse(NotaFiscalServico nfse) => db.NotasFiscais.Add(nfse);
    public void AdicionarDocumento(DocumentoFiscal documento) => db.DocumentosFiscais.Add(documento);
    public void AdicionarBoleto(Boleto boleto) => db.Boletos.Add(boleto);
    public void AdicionarPagamento(Pagamento pagamento) => db.Pagamentos.Add(pagamento);
    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
    public Task<int> ContarNfseAsync(CancellationToken ct = default) => db.NotasFiscais.IgnoreQueryFilters().CountAsync(ct);
}

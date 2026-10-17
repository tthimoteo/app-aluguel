using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Fiscal;
using MediatR;

namespace Aluguel.Application.Cobrancas.BaixarArquivoCobranca;

public sealed record BaixarArquivoCobrancaQuery(Guid Id, TipoDownloadCobranca Tipo) : IRequest<ArquivoCobrancaDto?>;

public sealed class BaixarArquivoCobrancaQueryHandler(
    ICobrancaRepository cobrancas,
    IClienteRepository clientes,
    IPlanoRepository planos,
    IFileStorage storage,
    ICurrentUser currentUser)
    : IRequestHandler<BaixarArquivoCobrancaQuery, ArquivoCobrancaDto?>
{
    public const string BucketNfseXml = "nfse-xml";
    public const string BucketNfsePdf = "nfse-pdf";
    public const string BucketBoletos = "boletos";

    public async Task<ArquivoCobrancaDto?> Handle(BaixarArquivoCobrancaQuery request, CancellationToken cancellationToken)
    {
        var nfse = await cobrancas.ObterNfseAsync(request.Id, cancellationToken);
        if (nfse is null || !EscopoCliente.PodeAcessar(currentUser, nfse.ClienteId))
            return null;

        await AcessoCobranca.GarantirPlanoPermiteAsync(
            currentUser, clientes, planos, nfse.ClienteId, cancellationToken);

        return request.Tipo switch
        {
            TipoDownloadCobranca.NfseXml => await BaixarDocumentoAsync(
                nfse.Id, TipoDocumentoFiscal.XML, BucketNfseXml, "application/xml",
                $"nfse-{nfse.Competencia.Replace('/', '-')}.xml", cancellationToken),
            TipoDownloadCobranca.NfsePdf => await BaixarDocumentoAsync(
                nfse.Id, TipoDocumentoFiscal.PDF, BucketNfsePdf, "application/pdf",
                $"nfse-{nfse.Competencia.Replace('/', '-')}.pdf", cancellationToken),
            TipoDownloadCobranca.BoletoPdf => await BaixarBoletoAsync(nfse.Id, nfse.Competencia, cancellationToken),
            _ => null,
        };
    }

    private async Task<ArquivoCobrancaDto?> BaixarDocumentoAsync(
        Guid nfseId, TipoDocumentoFiscal tipo, string bucket, string contentType, string nome,
        CancellationToken ct)
    {
        var doc = await cobrancas.ObterDocumentoAsync(nfseId, tipo, ct);
        if (doc is null) return null;

        var stream = await storage.DownloadAsync(bucket, doc.StoragePath, ct);
        return new ArquivoCobrancaDto(stream, contentType, nome);
    }

    private async Task<ArquivoCobrancaDto?> BaixarBoletoAsync(Guid nfseId, string competencia, CancellationToken ct)
    {
        var boleto = await cobrancas.ObterBoletoPorNfseAsync(nfseId, ct);
        if (boleto is null || !boleto.PossuiPdf) return null;

        var stream = await storage.DownloadAsync(BucketBoletos, boleto.StoragePathPdf!, ct);
        return new ArquivoCobrancaDto(stream, "application/pdf",
            $"boleto-{competencia.Replace('/', '-')}.pdf");
    }
}

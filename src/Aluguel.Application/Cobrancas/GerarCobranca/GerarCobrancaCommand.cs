using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Cobrancas.BaixarArquivoCobranca;
using Aluguel.Application.Common.Acesso;
using Aluguel.Domain.Contratos;
using Aluguel.Domain.Financeiro;
using Aluguel.Domain.Fiscal;
using MediatR;

namespace Aluguel.Application.Cobrancas.GerarCobranca;

/// <summary>
/// Gera cobrança de uma competência a partir do imóvel/contrato, com valores e flags editáveis na UI.
/// </summary>
public sealed record GerarCobrancaCommand(
    Guid ImovelId,
    string Competencia,
    decimal ValorAluguel,
    bool IncluirCondominio,
    decimal? ValorCondominio,
    bool IncluirIptu,
    decimal? ValorIptu,
    int DiaVencimento,
    bool EmitirNfse) : IRequest<CobrancaDetalheDto>;

public sealed class GerarCobrancaCommandHandler(
    IImovelRepository imoveis,
    IContratoRepository contratos,
    ICobrancaRepository cobrancas,
    IClienteRepository clientes,
    IPlanoRepository planos,
    IFileStorage storage,
    ICurrentUser currentUser)
    : IRequestHandler<GerarCobrancaCommand, CobrancaDetalheDto>
{
    private static readonly Regex CompetenciaRegex = new(
        @"^(0[1-9]|1[0-2])\/(20\d{2}|2100)$", RegexOptions.Compiled);

    public async Task<CobrancaDetalheDto> Handle(GerarCobrancaCommand request, CancellationToken cancellationToken)
    {
        var competencia = (request.Competencia ?? "").Trim();
        if (!CompetenciaRegex.IsMatch(competencia))
            throw new ArgumentException("Competência deve estar no formato MM/AAAA.");

        if (request.DiaVencimento is < 1 or > 31)
            throw new ArgumentException("Dia de cobrança deve estar entre 1 e 31.");

        if (request.ValorAluguel < 0)
            throw new ArgumentException("Valor do aluguel não pode ser negativo.");

        var condo = request.IncluirCondominio ? (request.ValorCondominio ?? 0m) : 0m;
        var iptu = request.IncluirIptu ? (request.ValorIptu ?? 0m) : 0m;
        if (condo < 0)
            throw new ArgumentException("Valor do condomínio não pode ser negativo.");
        if (iptu < 0)
            throw new ArgumentException("Valor do IPTU não pode ser negativo.");

        var valorTotal = request.ValorAluguel + condo + iptu;
        if (valorTotal <= 0)
            throw new InvalidOperationException("O valor total da cobrança deve ser maior que zero.");

        var imovel = await imoveis.ObterPorIdAsync(request.ImovelId, cancellationToken)
            ?? throw new InvalidOperationException("Imóvel não encontrado.");

        if (!EscopoCliente.PodeAcessar(currentUser, imovel.ClienteId))
            throw new InvalidOperationException("Sem permissão para gerar cobrança deste imóvel.");

        await AcessoCobranca.GarantirPlanoPermiteAsync(
            currentUser, clientes, planos, imovel.ClienteId, cancellationToken);

        if (imovel.Status != Domain.Common.StatusAtivoInativo.Ativo)
            throw new InvalidOperationException("Somente imóveis ativos podem gerar cobrança.");

        if (string.IsNullOrWhiteSpace(imovel.CompetenciaInicial))
            throw new InvalidOperationException(
                "Cadastre a competência inicial e os dados de cobrança do imóvel antes de gerar.");

        if (CompararCompetencia(competencia, imovel.CompetenciaInicial!) < 0)
            throw new InvalidOperationException(
                $"A competência não pode ser anterior à competência inicial do imóvel ({imovel.CompetenciaInicial}).");

        // Um imóvel não pode ter mais de uma cobrança na mesma competência (CASO 6/7/8).
        if (await cobrancas.ExisteCobrancaNaoCanceladaAsync(imovel.Id, competencia, cancellationToken))
            throw new InvalidOperationException(
                $"Já existe cobrança para o imóvel nesta competência ({competencia}).");

        var contratosAtivos = await contratos.ListarAsync(
            imovel.ClienteId, imovel.Id, null, StatusContrato.Ativo, 0, 1, cancellationToken);
        var contrato = contratosAtivos.FirstOrDefault()
            ?? throw new InvalidOperationException(
                "É necessário um contrato ativo no imóvel para gerar a cobrança.");

        var vencimento = CalcularVencimento(competencia, request.DiaVencimento);
        var agora = DateTimeOffset.UtcNow;
        var emissorId = currentUser.UserId ?? Guid.Empty;

        var nfse = new NotaFiscalServico(
            imovel.TenantId,
            imovel.ClienteId,
            imovel.Id,
            competencia,
            valorTotal,
            contratoId: contrato.Id,
            inquilinoId: contrato.InquilinoId);

        cobrancas.AdicionarNfse(nfse);

        var numero = Math.Abs(BitConverter.ToInt64(nfse.Id.ToByteArray(), 0) % 900_000) + 100_000;

        if (request.EmitirNfse)
        {
            nfse.MarcarEmitida(numero, "1",
                $"NFe{numero}{competencia.Replace("/", "", StringComparison.Ordinal)}",
                emissorId, agora);

            var xmlBytes = Encoding.UTF8.GetBytes(
                $"""<?xml version="1.0" encoding="UTF-8"?><NFSe><Numero>{numero}</Numero><Competencia>{competencia}</Competencia><Valor>{F(valorTotal)}</Valor><Aluguel>{F(request.ValorAluguel)}</Aluguel><Condominio>{F(condo)}</Condominio><Iptu>{F(iptu)}</Iptu></NFSe>""");
            var pdfBytes = Encoding.UTF8.GetBytes($"%PDF-1.4\n% NFS-e {numero} competencia {competencia}\n");

            var xmlRel = $"{imovel.TenantId:D}/{nfse.Id:D}.xml";
            var pdfRel = $"{imovel.TenantId:D}/{nfse.Id:D}.pdf";

            string xmlPath, pdfPath;
            await using (var s = new MemoryStream(xmlBytes))
                xmlPath = await storage.UploadAsync(BaixarArquivoCobrancaQueryHandler.BucketNfseXml, xmlRel, s, "application/xml", cancellationToken);
            await using (var s = new MemoryStream(pdfBytes))
                pdfPath = await storage.UploadAsync(BaixarArquivoCobrancaQueryHandler.BucketNfsePdf, pdfRel, s, "application/pdf", cancellationToken);

            cobrancas.AdicionarDocumento(new DocumentoFiscal(imovel.TenantId, nfse.Id, TipoDocumentoFiscal.XML, xmlPath, tamanhoBytes: xmlBytes.Length));
            cobrancas.AdicionarDocumento(new DocumentoFiscal(imovel.TenantId, nfse.Id, TipoDocumentoFiscal.PDF, pdfPath, tamanhoBytes: pdfBytes.Length));
        }

        var boletoBytes = Encoding.UTF8.GetBytes(
            $"%PDF-1.4\n% Boleto competencia {competencia} valor {F(valorTotal)} venc {vencimento:dd/MM/yyyy}\n");
        var boletoRel = $"{imovel.TenantId:D}/{nfse.Id:D}-boleto.pdf";
        string boletoPath;
        await using (var s = new MemoryStream(boletoBytes))
            boletoPath = await storage.UploadAsync(BaixarArquivoCobrancaQueryHandler.BucketBoletos, boletoRel, s, "application/pdf", cancellationToken);

        var boleto = new Boleto(
            imovel.TenantId,
            imovel.ClienteId,
            imovel.Id,
            competencia,
            valorTotal,
            vencimento,
            nfse.Id,
            nossoNumero: $"BOL{numero}",
            linhaDigitavel: $"23790.00000 00000.000000 00000.000000 {numero % 10} {numero}");
        boleto.DefinirPdf(boletoPath);
        cobrancas.AdicionarBoleto(boleto);

        await cobrancas.SalvarAlteracoesAsync(cancellationToken);

        return (await cobrancas.ObterDetalheAsync(nfse.Id, cancellationToken))!;
    }

    private static string F(decimal v) => v.ToString("F2", CultureInfo.InvariantCulture);

    private static int CompararCompetencia(string a, string b)
    {
        static (int Ano, int Mes) Parse(string c) =>
            (int.Parse(c.AsSpan(3, 4), CultureInfo.InvariantCulture),
                int.Parse(c.AsSpan(0, 2), CultureInfo.InvariantCulture));

        var (anoA, mesA) = Parse(a);
        var (anoB, mesB) = Parse(b);
        var cmp = anoA.CompareTo(anoB);
        return cmp != 0 ? cmp : mesA.CompareTo(mesB);
    }

    private static DateOnly CalcularVencimento(string competencia, int dia)
    {
        var mes = int.Parse(competencia.AsSpan(0, 2), CultureInfo.InvariantCulture);
        var ano = int.Parse(competencia.AsSpan(3, 4), CultureInfo.InvariantCulture);
        var diaOk = Math.Clamp(dia, 1, DateTime.DaysInMonth(ano, mes));
        return new DateOnly(ano, mes, diaOk);
    }
}

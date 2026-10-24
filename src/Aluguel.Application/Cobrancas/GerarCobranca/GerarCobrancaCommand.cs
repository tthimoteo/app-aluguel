using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Aluguel.Application.Abstractions;
using Aluguel.Application.Cobrancas.BaixarArquivoCobranca;
using Aluguel.Application.Clientes.EnviarCertificadoCliente;
using Aluguel.Application.Common.Acesso;
using Aluguel.Application.Common.Validacoes;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Contratos;
using Aluguel.Domain.Financeiro;
using Aluguel.Domain.Fiscal;
using MediatR;

namespace Aluguel.Application.Cobrancas.GerarCobranca;

/// <summary>
/// Gera cobrança de uma competência a partir do imóvel/contrato, com valores e flags editáveis na UI.
/// Quando <c>EmitirNfse</c>, chama <see cref="INfseNacionalProvider"/> (SEFIN ou mock).
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
    IInquilinoRepository inquilinos,
    ICertificadoDigitalRepository certificados,
    INfseNacionalProvider nfseProvider,
    INfsePdfGenerator nfsePdf,
    IFileStorage storage,
    ISecretProtector protector,
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

        var vencimento = CobrancaVencimento.Calcular(competencia, request.DiaVencimento);
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

        var numeroFallback = Math.Abs(BitConverter.ToInt64(nfse.Id.ToByteArray(), 0) % 900_000) + 100_000;
        long numero = numeroFallback;

        if (request.EmitirNfse)
        {
            GarantirCompetenciaNaoFuturaParaEmissao(competencia);

            var emissao = await EmitirNfseNacionalAsync(
                nfse, imovel, contrato.InquilinoId, competencia, valorTotal,
                request.ValorAluguel, condo, iptu, cancellationToken);

            if (!emissao.Sucesso || emissao.XmlNfse is null || string.IsNullOrWhiteSpace(emissao.ChaveAcesso))
                throw new InvalidOperationException(
                    emissao.MensagemErro ?? "Falha ao emitir NFS-e na API Nacional.");

            numero = emissao.Numero ?? numeroFallback;
            nfse.MarcarEmitida(
                numero,
                string.IsNullOrWhiteSpace(emissao.Serie) ? "1" : emissao.Serie!,
                emissao.ChaveAcesso!,
                emissorId,
                agora);

            var xmlBytes = emissao.XmlNfse;
            var pdfBytes = nfsePdf.Gerar(xmlBytes, emissao.ChaveAcesso!, competencia);

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

    private async Task<NfseEmissaoResultado> EmitirNfseNacionalAsync(
        NotaFiscalServico nfse,
        Domain.Imoveis.Imovel imovel,
        Guid inquilinoId,
        string competencia,
        decimal valorTotal,
        decimal valorAluguel,
        decimal condo,
        decimal iptu,
        CancellationToken ct)
    {
        var clienteId = imovel.ClienteId;
        var cliente = await clientes.ObterPorIdAsync(clienteId, ct)
            ?? throw new InvalidOperationException("Cliente emitente não encontrado.");

        var cTribNac = NfseCodigoFiscal.NormalizarDigitos(cliente.CodigoServico, 6)
                       ?? NfseCodigosPadrao.CodigoTributacaoNacional;
        var cIndOp = NfseCodigoFiscal.NormalizarDigitos(cliente.CodigoIndicadorOperacao, 6)
                     ?? NfseCodigosPadrao.CodigoIndicadorOperacao;
        var cClassTrib = NfseCodigoFiscal.NormalizarDigitos(cliente.ClassificacaoTributaria, 6)
                         ?? NfseCodigosPadrao.ClassificacaoTributaria;

        if (string.IsNullOrWhiteSpace(cliente.InscricaoMunicipal))
            throw new InvalidOperationException(
                "Cadastre a inscrição municipal do cliente antes de emitir a NFS-e.");

        var cert = await certificados.ObterAtivoPorClienteAsync(clienteId, ct)
            ?? throw new InvalidOperationException(
                "Envie o certificado digital A1 do cliente antes de emitir a NFS-e.");

        if (cert.Validade < DateTimeOffset.UtcNow)
            throw new InvalidOperationException("O certificado digital A1 do cliente está vencido.");

        var inquilino = await inquilinos.ObterPorIdAsync(inquilinoId, ct)
            ?? throw new InvalidOperationException("Inquilino (tomador) da cobrança não encontrado.");

        await using var pfxStream = await storage.DownloadAsync(
            EnviarCertificadoClienteCommandHandler.Bucket, cert.StoragePath, ct);
        await using var pfxBuffer = new MemoryStream();
        await pfxStream.CopyToAsync(pfxBuffer, ct);
        var pfxBytes = pfxBuffer.ToArray();
        if (pfxBytes.Length == 0)
            throw new InvalidOperationException("Arquivo do certificado A1 está vazio no storage.");

        string senha;
        try
        {
            senha = protector.Unprotect(cert.SenhaCifrada);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Não foi possível recuperar a senha do certificado A1.");
        }

        var cMunPrest = ResolverMunicipio(cliente.Endereco.Cidade, cliente.Endereco.Uf);
        var cMunToma = ResolverMunicipio(inquilino.Endereco.Cidade, inquilino.Endereco.Uf) ?? cMunPrest;

        var desc = MontarDescricaoServico(competencia, valorAluguel, condo, iptu);
        var docPrest = cliente.TipoPessoa == TipoPessoa.PJ ? cliente.Cnpj! : cliente.Cpf!;
        var regime = cliente.RegimeTributario?.ToString();

        var pedido = new NfseEmissaoPedido(
            nfse.Id,
            competencia,
            valorTotal,
            desc,
            new NfsePartePrestador(
                Documento: docPrest,
                PessoaJuridica: cliente.TipoPessoa == TipoPessoa.PJ,
                InscricaoMunicipal: cliente.InscricaoMunicipal,
                RazaoSocial: cliente.TipoPessoa == TipoPessoa.PJ ? cliente.RazaoSocial : cliente.Nome,
                Telefone: cliente.Telefone,
                Email: cliente.Email,
                CodigoServico: cTribNac,
                CodigoIndicadorOperacao: cIndOp,
                ClassificacaoTributaria: cClassTrib,
                RegimeTributario: regime,
                CodigoMunicipioIbge: cMunPrest,
                Cep: cliente.Endereco.Cep,
                Logradouro: cliente.Endereco.Logradouro,
                Numero: cliente.Endereco.Numero,
                Complemento: cliente.Endereco.Complemento,
                Bairro: cliente.Endereco.Bairro),
            new NfseParteTomador(
                Documento: inquilino.Documento,
                PessoaJuridica: inquilino.TipoPessoa == TipoPessoa.PJ,
                Nome: inquilino.Nome,
                InscricaoMunicipal: inquilino.InscricaoMunicipal,
                Telefone: inquilino.Telefone,
                Email: inquilino.Email,
                CodigoMunicipioIbge: cMunToma,
                Cep: inquilino.Endereco.Cep,
                Logradouro: inquilino.Endereco.Logradouro,
                Numero: inquilino.Endereco.Numero,
                Complemento: inquilino.Endereco.Complemento,
                Bairro: inquilino.Endereco.Bairro),
            pfxBytes,
            senha,
            Imovel: new NfseDadosImovel(
                CodigoMunicipioIbge: ResolverMunicipio(imovel.Endereco.Cidade, imovel.Endereco.Uf),
                Cep: imovel.Endereco.Cep,
                Logradouro: imovel.Endereco.Logradouro,
                Numero: imovel.Endereco.Numero,
                Complemento: imovel.Endereco.Complemento,
                Bairro: imovel.Endereco.Bairro,
                InscricaoImobiliaria: imovel.NumeroIptu,
                CodigoNbs: imovel.Tipo == Domain.Imoveis.TipoImovel.Comercial
                    ? NfseCodigosPadrao.CodigoNbsNaoResidencial
                    : NfseCodigosPadrao.CodigoNbsResidencial));

        return await nfseProvider.EmitirAsync(pedido, ct);
    }

    /// <summary>
    /// IBGE do município a partir de cidade/UF. Fallback São Paulo só quando cidade vazia —
    /// cidade conhecida errada (ex. São Caetano → SP capital) gera E0240 na SEFIN.
    /// </summary>
    private static string ResolverMunicipio(string? cidade, string? uf)
    {
        if (string.IsNullOrWhiteSpace(cidade) || string.IsNullOrWhiteSpace(uf))
            return "3550308";

        var c = cidade.Trim().ToLowerInvariant()
            .Replace("á", "a").Replace("à", "a").Replace("ã", "a").Replace("â", "a")
            .Replace("é", "e").Replace("ê", "e")
            .Replace("í", "i")
            .Replace("ó", "o").Replace("ô", "o").Replace("õ", "o")
            .Replace("ú", "u").Replace("ü", "u")
            .Replace("ç", "c");
        var u = uf.Trim().ToUpperInvariant();

        return (u, c) switch
        {
            ("SP", "sao paulo") => "3550308",
            ("SP", "sao caetano do sul") => "3548807",
            ("SP", "santo andre") => "3547809",
            ("SP", "sao bernardo do campo") => "3548708",
            ("SP", "guarulhos") => "3518800",
            ("SP", "osasco") => "3534401",
            ("SP", "campinas") => "3509502",
            ("RJ", "rio de janeiro") => "3304557",
            ("MG", "belo horizonte") => "3106200",
            ("DF", "brasilia") => "5300108",
            ("PR", "curitiba") => "4106902",
            ("RS", "porto alegre") => "4314902",
            ("BA", "salvador") => "2927408",
            ("CE", "fortaleza") => "2304400",
            ("PE", "recife") => "2611606",
            _ => "3550308",
        };
    }

    private static string MontarDescricaoServico(
        string competencia, decimal aluguel, decimal condo, decimal iptu)
    {
        var partes = new List<string> { $"Aluguel competência {competencia}: R$ {F(aluguel)}" };
        if (condo > 0) partes.Add($"Condomínio: R$ {F(condo)}");
        if (iptu > 0) partes.Add($"IPTU: R$ {F(iptu)}");
        return string.Join("; ", partes);
    }

    /// <summary>
    /// SEFIN E0015: <c>dCompet</c> não pode ser posterior a <c>dhEmi</c>.
    /// Usa o 1º dia da competência vs. hoje em America/Sao_Paulo.
    /// </summary>
    private static void GarantirCompetenciaNaoFuturaParaEmissao(string competencia)
    {
        var mes = int.Parse(competencia.AsSpan(0, 2), CultureInfo.InvariantCulture);
        var ano = int.Parse(competencia.AsSpan(3, 4), CultureInfo.InvariantCulture);
        var dCompet = new DateOnly(ano, mes, 1);
        var hoje = DateOnly.FromDateTime(ObterAgoraSaoPaulo().DateTime);
        if (dCompet > hoje)
            throw new InvalidOperationException(
                $"Não é possível emitir NFS-e para competência futura ({competencia}). " +
                "A data de competência (1º dia do mês) não pode ser posterior à data de emissão.");
    }

    private static DateTimeOffset ObterAgoraSaoPaulo()
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            return TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz);
        }
        catch (TimeZoneNotFoundException)
        {
            return DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-3));
        }
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
}

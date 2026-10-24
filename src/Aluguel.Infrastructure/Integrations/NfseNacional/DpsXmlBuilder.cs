using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Aluguel.Application.Abstractions;

namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>Monta o XML da DPS (layout v1.01) antes da assinatura XMLDSig.</summary>
public static class DpsXmlBuilder
{
    public static readonly XNamespace Ns = "http://www.sped.fazenda.gov.br/nfse";

    public static string Montar(
        NfseEmissaoPedido pedido,
        NfseOptions options,
        string numeroDps,
        DateTimeOffset emitidoEm,
        out string idDps)
    {
        ArgumentNullException.ThrowIfNull(pedido);
        ArgumentNullException.ThrowIfNull(options);

        var serie = Digitos(options.SeriePadrao, 1, 5);
        var nDps = NormalizarNumeroDps(numeroDps);
        var cMun = Digitos(pedido.Prestador.CodigoMunicipioIbge, 7, 7);
        var docPrest = Digitos(pedido.Prestador.Documento, pedido.Prestador.PessoaJuridica ? 14 : 11,
            pedido.Prestador.PessoaJuridica ? 14 : 11);
        var tpInsc = pedido.Prestador.PessoaJuridica ? "2" : "1";

        idDps = MontarId(cMun, tpInsc, docPrest, serie, nDps);

        var (cTribNac, cTribMun) = ResolverCodigosServico(pedido.Prestador.CodigoServico, options);
        var competencia = ParseCompetencia(pedido.Competencia);
        var dhEmi = emitidoEm.ToOffset(ObterOffsetSaoPaulo(emitidoEm)).ToString(
            "yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        var verAplic = Truncar(options.VerAplic, 20);
        var valor = pedido.ValorServico.ToString("F2", CultureInfo.InvariantCulture);

        var prest = new XElement(Ns + "prest");
        if (pedido.Prestador.PessoaJuridica)
            prest.Add(new XElement(Ns + "CNPJ", docPrest));
        else
            prest.Add(new XElement(Ns + "CPF", docPrest));

        // E0120: muitos municípios em homologação rejeitam IM sem CNC complementar.
        if (options.EnviarImPrestador)
        {
            var im = SomenteDigitos(pedido.Prestador.InscricaoMunicipal);
            if (!string.IsNullOrEmpty(im))
                prest.Add(new XElement(Ns + "IM", im));
        }

        // E0121/E0128: com tpEmit=1 (prestador emitente) não informar xNome, endereço, fone nem e-mail
        // do prestador — a SEFIN usa o cadastro CNC/RFB.
        prest.Add(MontarRegTrib(pedido.Prestador.RegimeTributario));

        var toma = new XElement(Ns + "toma");
        var docToma = Digitos(pedido.Tomador.Documento, pedido.Tomador.PessoaJuridica ? 14 : 11,
            pedido.Tomador.PessoaJuridica ? 14 : 11);
        if (pedido.Tomador.PessoaJuridica)
            toma.Add(new XElement(Ns + "CNPJ", docToma));
        else
            toma.Add(new XElement(Ns + "CPF", docToma));

        var imT = SomenteDigitos(pedido.Tomador.InscricaoMunicipal);
        if (!string.IsNullOrEmpty(imT))
            toma.Add(new XElement(Ns + "IM", imT));

        toma.Add(new XElement(Ns + "xNome", Truncar(pedido.Tomador.Nome, 150)));

        var cMunToma = string.IsNullOrWhiteSpace(pedido.Tomador.CodigoMunicipioIbge)
            ? cMun
            : Digitos(pedido.Tomador.CodigoMunicipioIbge, 7, 7);
        AdicionarEndereco(toma, cMunToma, pedido.Tomador.Cep, pedido.Tomador.Logradouro,
            pedido.Tomador.Numero, pedido.Tomador.Complemento, pedido.Tomador.Bairro);

        var foneT = SomenteDigitos(pedido.Tomador.Telefone);
        if (!string.IsNullOrEmpty(foneT))
            toma.Add(new XElement(Ns + "fone", Truncar(foneT, 20)));
        if (!string.IsNullOrWhiteSpace(pedido.Tomador.Email))
            toma.Add(new XElement(Ns + "email", Truncar(pedido.Tomador.Email!, 80)));

        // Ordem XSD NT 009 (prod. restrita 20/10/2026): cTribNac, cTribMun?, cNBS?, …, xDescServ.
        var cServ = new XElement(Ns + "cServ",
            new XElement(Ns + "cTribNac", cTribNac));
        if (!string.IsNullOrEmpty(cTribMun))
            cServ.Add(new XElement(Ns + "cTribMun", cTribMun));
        // E0322: com IBS/CBS a SEFIN exige cNBS. Locação 990301 → 110021000 / 110022000 (NT 005).
        if (options.IncluirIbsCbs)
        {
            var nbsBruto = pedido.Imovel?.CodigoNbs ?? options.CodigoNbsPadrao;
            if (SomenteDigitos(nbsBruto) is { Length: 9 } nbs)
                cServ.Add(new XElement(Ns + "cNBS", nbs));
        }
        cServ.Add(new XElement(Ns + "xDescServ", Truncar(pedido.DescricaoServico, 2000)));

        // E0532: serviços 99.XX.XX (ex. 990301 locação) → tribISSQN = 4 (Não incidência).
        var tribIssqn = cTribNac.StartsWith("99", StringComparison.Ordinal) ? "4" : "1";
        var tribMun = new XElement(Ns + "tribMun",
            new XElement(Ns + "tribISSQN", tribIssqn),
            new XElement(Ns + "tpRetISSQN", "1"));

        var totTrib = MontarTotTrib(pedido.Prestador.RegimeTributario, options);

        var valores = new XElement(Ns + "valores",
            new XElement(Ns + "vServPrest",
                new XElement(Ns + "vServ", valor)),
            new XElement(Ns + "trib",
                tribMun,
                totTrib));

        var infDps = new XElement(Ns + "infDPS",
            new XAttribute("Id", idDps),
            new XElement(Ns + "tpAmb", options.TipoAmbiente.ToString(CultureInfo.InvariantCulture)),
            new XElement(Ns + "dhEmi", dhEmi),
            new XElement(Ns + "verAplic", verAplic),
            new XElement(Ns + "serie", serie),
            new XElement(Ns + "nDPS", nDps),
            new XElement(Ns + "dCompet", competencia.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)),
            new XElement(Ns + "tpEmit", "1"),
            // NT 009 (homologação 20/10/2026): finNFSe sai de IBSCBS e fica obrigatório em infDPS, antes de cLocEmi.
            new XElement(Ns + "finNFSe", string.IsNullOrWhiteSpace(options.FinNfse) ? "0" : options.FinNfse.Trim()),
            new XElement(Ns + "cLocEmi", cMun),
            prest,
            toma,
            new XElement(Ns + "serv",
                new XElement(Ns + "locPrest",
                    new XElement(Ns + "cLocPrestacao", cMun)),
                cServ),
            valores);

        if (options.IncluirIbsCbs)
            infDps.Add(MontarIbsCbs(pedido, options));

        var dps = new XElement(Ns + "DPS",
            new XAttribute("xmlns", Ns.NamespaceName),
            new XAttribute("versao", "1.01"),
            infDps);

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), dps);
        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            Indent = false,
            OmitXmlDeclaration = false
        }))
        {
            doc.Save(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static string MontarId(string cMun, string tpInsc, string documento14, string serie, string nDps)
    {
        var doc = documento14.PadLeft(14, '0');
        var ser = serie.PadLeft(5, '0');
        var num = nDps.PadLeft(15, '0');
        return string.Concat("DPS", cMun, tpInsc, doc, ser, num);
    }

    public static (string CTribNac, string? CTribMun) ResolverCodigosServico(string? codigoServico, NfseOptions options)
    {
        var digitos = SomenteDigitos(codigoServico) ?? "";
        if (digitos.Length == 6)
            return (digitos, null);

        var nac = Digitos(options.CodigoTributacaoNacionalPadrao, 6, 6);
        // XSD TCCodTribMun: exatamente 3 dígitos. Códigos municipais longos (ex. SP 5 dígitos)
        // ficam só no cadastro; não entram em cTribMun.
        var mun = digitos.Length == 3 ? digitos : null;
        return (nac, mun);
    }

    public static string GerarNumeroDps(Guid nfseId)
    {
        // Sempre 15 dígitos, sem zero à esquerda (XSD TSNumDPS / Id).
        var u = BitConverter.ToUInt64(nfseId.ToByteArray().AsSpan(0, 8));
        u = u % 900_000_000_000_000UL + 100_000_000_000_000UL;
        return u.ToString(CultureInfo.InvariantCulture);
    }

    private static XElement MontarRegTrib(string? regime)
    {
        // opSimpNac: 1=não optante, 2=MEI, 3=ME/EPP
        var r = (regime ?? "").Trim();
        if (string.Equals(r, "Mei", StringComparison.OrdinalIgnoreCase))
        {
            return new XElement(Ns + "regTrib",
                new XElement(Ns + "opSimpNac", "2"),
                new XElement(Ns + "regEspTrib", "0"));
        }

        if (string.Equals(r, "SimplesNacional", StringComparison.OrdinalIgnoreCase))
        {
            return new XElement(Ns + "regTrib",
                new XElement(Ns + "opSimpNac", "3"),
                new XElement(Ns + "regApTribSN", "1"),
                new XElement(Ns + "regEspTrib", "0"));
        }

        return new XElement(Ns + "regTrib",
            new XElement(Ns + "opSimpNac", "1"),
            new XElement(Ns + "regEspTrib", "0"));
    }

    private static XElement MontarTotTrib(string? regime, NfseOptions options)
    {
        if (string.Equals(regime, "SimplesNacional", StringComparison.OrdinalIgnoreCase))
        {
            var p = options.PercentualTotTribSn.ToString("F2", CultureInfo.InvariantCulture);
            return new XElement(Ns + "totTrib",
                new XElement(Ns + "pTotTribSN", p));
        }

        return new XElement(Ns + "totTrib",
            new XElement(Ns + "indTotTrib", "0"));
    }

    /// <summary>
    /// Grupo IBS/CBS (NT 009 / layout 20/10/2026): cIndOp, indDest, imovel (locação), valores/trib.
    /// </summary>
    private static XElement MontarIbsCbs(NfseEmissaoPedido pedido, NfseOptions options)
    {
        var prestador = pedido.Prestador;
        var cClass = SomenteDigitos(prestador.ClassificacaoTributaria)
                     ?? Digitos(options.ClassificacaoTributariaIbsCbs, 6, 6);
        if (cClass.Length != 6)
            cClass = Digitos(options.ClassificacaoTributariaIbsCbs, 6, 6);

        var cInd = SomenteDigitos(prestador.CodigoIndicadorOperacao)
                   ?? options.CodigoIndicadorOperacaoIbsCbs;
        cInd = string.IsNullOrWhiteSpace(cInd) ? null : Digitos(cInd, 6, 6);

        // E0959: CST = 3 primeiros dígitos de cClassTrib (ex.: 200027 → 200).
        var cst = cClass.Length >= 3 ? cClass[..3] : Digitos(options.CstIbsCbs, 3, 3);
        var trib = new XElement(Ns + "trib",
            new XElement(Ns + "CST", cst),
            new XElement(Ns + "cClassTrib", cClass));

        var ibs = new XElement(Ns + "IBSCBS");
        if (!string.IsNullOrWhiteSpace(cInd))
            ibs.Add(new XElement(Ns + "cIndOp", cInd));
        ibs.Add(new XElement(Ns + "indDest", "0"));

        // E0932: cIndOp de imóvel (020101/020201/…) exige grupo imovel.
        if (cInd is not null && cInd.StartsWith("02", StringComparison.Ordinal) && pedido.Imovel is not null)
            ibs.Add(MontarGrupoImovel(pedido.Imovel, pedido.ValorServico));

        ibs.Add(new XElement(Ns + "valores", trib));
        return ibs;
    }

    private static XElement MontarGrupoImovel(NfseDadosImovel imovel, decimal valorOperacao)
    {
        var cMun = Digitos(imovel.CodigoMunicipioIbge, 7, 7);
        var pCop = imovel.PercentualCopropriedade.ToString("F2", CultureInfo.InvariantCulture);
        var vTot = valorOperacao.ToString("F2", CultureInfo.InvariantCulture);

        var grupo = new XElement(Ns + "imovel",
            new XElement(Ns + "cMun", cMun),
            new XElement(Ns + "gLocacao",
                new XElement(Ns + "pCopropriedade", pCop),
                new XElement(Ns + "vTotOper", vTot)));

        if (!string.IsNullOrWhiteSpace(imovel.InscricaoImobiliaria))
            grupo.AddFirst(new XElement(Ns + "inscImobFisc", Truncar(imovel.InscricaoImobiliaria.Trim(), 30)));

        var cep = SomenteDigitos(imovel.Cep);
        var lgr = imovel.Logradouro?.Trim();
        var nro = string.IsNullOrWhiteSpace(imovel.Numero) ? "S/N" : imovel.Numero.Trim();
        var bai = imovel.Bairro?.Trim();
        if (!string.IsNullOrEmpty(cep) && !string.IsNullOrWhiteSpace(lgr) && !string.IsNullOrWhiteSpace(bai))
        {
            var end = new XElement(Ns + "end",
                new XElement(Ns + "CEP", Digitos(cep, 8, 8)),
                new XElement(Ns + "xLgr", Truncar(lgr, 255)),
                new XElement(Ns + "nro", Truncar(nro, 60)));
            if (!string.IsNullOrWhiteSpace(imovel.Complemento))
                end.Add(new XElement(Ns + "xCpl", Truncar(imovel.Complemento.Trim(), 156)));
            end.Add(new XElement(Ns + "xBairro", Truncar(bai, 60)));
            grupo.Add(new XElement(Ns + "gUnidImob", end));
        }

        return grupo;
    }

    private static void AdicionarEndereco(
        XElement parent,
        string? cMun,
        string? cep,
        string? logradouro,
        string? numero,
        string? complemento,
        string? bairro)
    {
        var cepDig = SomenteDigitos(cep);
        var lgr = logradouro?.Trim();
        var nro = string.IsNullOrWhiteSpace(numero) ? "S/N" : numero.Trim();
        var bai = bairro?.Trim();

        if (string.IsNullOrEmpty(cepDig) || string.IsNullOrWhiteSpace(lgr) ||
            string.IsNullOrWhiteSpace(bai) || string.IsNullOrWhiteSpace(cMun))
            return;

        var end = new XElement(Ns + "end",
            new XElement(Ns + "endNac",
                new XElement(Ns + "cMun", Digitos(cMun, 7, 7)),
                new XElement(Ns + "CEP", Digitos(cepDig, 8, 8))),
            new XElement(Ns + "xLgr", Truncar(lgr, 255)),
            new XElement(Ns + "nro", Truncar(nro, 60)));

        if (!string.IsNullOrWhiteSpace(complemento))
            end.Add(new XElement(Ns + "xCpl", Truncar(complemento.Trim(), 156)));

        end.Add(new XElement(Ns + "xBairro", Truncar(bai, 60)));
        parent.Add(end);
    }

    private static DateOnly ParseCompetencia(string competencia)
    {
        var mes = int.Parse(competencia.AsSpan(0, 2), CultureInfo.InvariantCulture);
        var ano = int.Parse(competencia.AsSpan(3, 4), CultureInfo.InvariantCulture);
        return new DateOnly(ano, mes, 1);
    }

    private static TimeSpan ObterOffsetSaoPaulo(DateTimeOffset utcLike)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
            return tz.GetUtcOffset(utcLike.UtcDateTime);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeSpan.FromHours(-3);
        }
    }

    private static string NormalizarNumeroDps(string numero)
    {
        var digitos = SomenteDigitos(numero)
            ?? throw new InvalidOperationException("Número da DPS inválido.");
        if (digitos.Length is < 1 or > 15 || digitos[0] == '0')
            throw new InvalidOperationException("Número da DPS deve ter 1–15 dígitos e não começar com zero.");
        return digitos;
    }

    private static string Digitos(string? valor, int min, int max)
    {
        var d = SomenteDigitos(valor)
            ?? throw new InvalidOperationException($"Valor numérico obrigatório ({min}-{max} dígitos).");
        if (d.Length < min || d.Length > max)
            throw new InvalidOperationException($"Valor '{d}' deve ter entre {min} e {max} dígitos.");
        return d;
    }

    private static string? SomenteDigitos(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : new string(valor.Where(char.IsDigit).ToArray());

    private static string Truncar(string valor, int max) =>
        valor.Length <= max ? valor : valor[..max];
}

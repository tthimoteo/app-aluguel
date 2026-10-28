using System.Globalization;
using System.Xml.Linq;

namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>Extrai do XML autorizado da NFS-e os campos previstos na NT 008 para o DANFSe.</summary>
internal static class NfseDanfseXmlParser
{
    private static readonly XNamespace Ns = "http://www.sped.fazenda.gov.br/nfse";

    public static NfseDanfseModelo Parse(byte[] xmlNfseAutorizado, string? chaveFallback, string? competenciaFallback)
    {
        using var stream = new MemoryStream(xmlNfseAutorizado);
        var doc = XDocument.Load(stream);
        var inf = doc.Descendants(Ns + "infNFSe").FirstOrDefault()
                  ?? throw new InvalidOperationException("XML da NFS-e sem infNFSe.");
        var dps = inf.Element(Ns + "DPS")?.Element(Ns + "infDPS");
        var emit = inf.Element(Ns + "emit");
        var prest = dps?.Element(Ns + "prest");
        var toma = dps?.Element(Ns + "toma");
        var serv = dps?.Element(Ns + "serv");
        var cServ = serv?.Element(Ns + "cServ");
        var tribMun = dps?.Element(Ns + "valores")?.Element(Ns + "trib")?.Element(Ns + "tribMun");
        var totTrib = dps?.Element(Ns + "valores")?.Element(Ns + "trib")?.Element(Ns + "totTrib");
        var ibsNfse = inf.Element(Ns + "IBSCBS");
        var ibsDps = dps?.Element(Ns + "IBSCBS");
        var imovel = ibsDps?.Element(Ns + "imovel");

        var id = inf.Attribute("Id")?.Value ?? "";
        var chave = !string.IsNullOrWhiteSpace(chaveFallback)
            ? SomenteDigitos(chaveFallback)!
            : id.StartsWith("NFS", StringComparison.OrdinalIgnoreCase) && id.Length > 3
                ? id[3..]
                : SomenteDigitos(id) ?? "";

        var dCompet = T(dps, "dCompet");
        var competencia = competenciaFallback ?? "";
        var competenciaIso = dCompet ?? "";
        if (DateOnly.TryParse(dCompet, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dc))
            competencia = $"{dc.Day:00}/{dc.Month:00}/{dc.Year}";
        else if (!string.IsNullOrWhiteSpace(competenciaFallback) && competenciaFallback.Contains('/'))
        {
            // fallback MM/AAAA → 01/MM/AAAA
            if (competenciaFallback.Length >= 7
                && int.TryParse(competenciaFallback.AsSpan(0, 2), out var m)
                && int.TryParse(competenciaFallback.AsSpan(3, 4), out var a))
            {
                competencia = $"01/{m:00}/{a}";
                competenciaIso = $"{a:0000}-{m:00}-01";
            }
        }

        var tpAmb = T(dps, "tpAmb") ?? "1";
        var homologacao = tpAmb == "2";
        var ambGer = T(inf, "ambGer");
        var cStat = T(inf, "cStat") ?? "";
        var fin = T(dps, "finNFSe") ?? "0";
        var tpEmit = T(dps, "tpEmit") ?? "1";
        var tribIssqn = T(tribMun, "tribISSQN") ?? "";

        var vServ = T(dps?.Element(Ns + "valores")?.Element(Ns + "vServPrest"), "vServ");
        var vLiq = T(inf.Element(Ns + "valores"), "vLiq") ?? vServ;
        var vTotNf = T(ibsNfse?.Element(Ns + "totCIBS"), "vTotNF") ?? vLiq;
        var vIbs = T(ibsNfse?.Element(Ns + "totCIBS")?.Element(Ns + "gIBS"), "vIBSTot");
        var vCbs = T(ibsNfse?.Element(Ns + "totCIBS")?.Element(Ns + "gCBS"), "vCBS");
        decimal.TryParse(vIbs, NumberStyles.Number, CultureInfo.InvariantCulture, out var ibsDec);
        decimal.TryParse(vCbs, NumberStyles.Number, CultureInfo.InvariantCulture, out var cbsDec);
        var totalIbsCbs = (ibsDec + cbsDec).ToString("F2", CultureInfo.InvariantCulture);

        var cst = T(ibsDps?.Element(Ns + "valores")?.Element(Ns + "trib"), "CST") ?? "";
        var cClass = T(ibsDps?.Element(Ns + "valores")?.Element(Ns + "trib"), "cClassTrib") ?? "";

        var consulta = $"https://www.nfse.gov.br/ConsultaPublica/?tpc=1&chave={chave}";

        return new NfseDanfseModelo
        {
            ChaveAcesso = chave,
            NumeroNfse = T(inf, "nNFSe") ?? "—",
            Competencia = string.IsNullOrWhiteSpace(competencia) ? "—" : competencia,
            CompetenciaIso = competenciaIso,
            DhProcessamento = FormatDateTime(T(inf, "dhProc")),
            NumeroDps = T(dps, "nDPS") ?? "—",
            SerieDps = T(dps, "serie") ?? "—",
            DhEmissaoDps = FormatDateTime(T(dps, "dhEmi")),
            EmitenteDescricao = DescreverTpEmit(tpEmit),
            SituacaoDescricao = DescreverCStat(cStat),
            FinalidadeDescricao = DescreverFinNfse(fin),
            LocalEmissao = T(inf, "xLocEmi") ?? "—",
            AmbienteGerador = DescreverAmbGer(ambGer),
            TipoAmbiente = homologacao ? "Produção Restrita (Homologação)" : "Produção",
            Homologacao = homologacao,
            ConsultaPublicaUrl = consulta,

            Prestador = MontarParte(emit, prest, ehPrestador: true),
            Tomador = toma is null ? null : MontarParte(null, toma, ehPrestador: false),

            TributacaoNacionalCodigo = T(cServ, "cTribNac") ?? "—",
            TributacaoNacionalDescricao = T(inf, "xTribNac") ?? "—",
            CodigoNbs = T(cServ, "cNBS") ?? "—",
            DescricaoNbs = T(inf, "xNBS") ?? "—",
            LocalPrestacao = T(inf, "xLocPrestacao") ?? T(inf, "xLocEmi") ?? "—",
            DescricaoServico = T(cServ, "xDescServ") ?? "—",

            TribIssqnCodigo = tribIssqn,
            TribIssqnDescricao = DescreverTribIssqn(tribIssqn),
            SemIssqn = tribIssqn is "2" or "3" or "4",
            RetencaoIssqn = DescreverRetIssqn(T(tribMun, "tpRetISSQN")),
            PercentualTotTribSn = T(totTrib, "pTotTribSN") ?? "",

            Cst = cst,
            ClassificacaoTributaria = cClass,
            CodigoIndicadorOperacao = T(ibsDps, "cIndOp") ?? "—",
            LocalidadeIncidenciaIbs = Concat(
                T(ibsNfse, "cLocalidadeIncid"),
                T(ibsNfse, "xLocalidadeIncid")),
            BaseCalculoIbsCbs = FormatMoney(T(ibsNfse?.Element(Ns + "valores"), "vBC")),
            AliquotaIbsUf = FormatPct(T(ibsNfse?.Element(Ns + "valores")?.Element(Ns + "uf"), "pIBSUF")),
            ReducaoAliqIbsUf = FormatPct(T(ibsNfse?.Element(Ns + "valores")?.Element(Ns + "uf"), "pRedAliqUF")),
            AliquotaEfetivaIbsUf = FormatPct(T(ibsNfse?.Element(Ns + "valores")?.Element(Ns + "uf"), "pAliqEfetUF")),
            ValorIbsUf = FormatMoney(T(ibsNfse?.Element(Ns + "totCIBS")?.Element(Ns + "gIBS")?.Element(Ns + "gIBSUFTot"), "vIBSUF")),
            AliquotaIbsMun = FormatPct(T(ibsNfse?.Element(Ns + "valores")?.Element(Ns + "mun"), "pIBSMun")),
            ReducaoAliqIbsMun = FormatPct(T(ibsNfse?.Element(Ns + "valores")?.Element(Ns + "mun"), "pRedAliqMun")),
            AliquotaEfetivaIbsMun = FormatPct(T(ibsNfse?.Element(Ns + "valores")?.Element(Ns + "mun"), "pAliqEfetMun")),
            ValorIbsMun = FormatMoney(T(ibsNfse?.Element(Ns + "totCIBS")?.Element(Ns + "gIBS")?.Element(Ns + "gIBSMunTot"), "vIBSMun")),
            ValorTotalIbs = FormatMoney(vIbs),
            AliquotaCbs = FormatPct(T(ibsNfse?.Element(Ns + "valores")?.Element(Ns + "fed"), "pCBS")),
            ReducaoAliqCbs = FormatPct(T(ibsNfse?.Element(Ns + "valores")?.Element(Ns + "fed"), "pRedAliqCBS")),
            AliquotaEfetivaCbs = FormatPct(T(ibsNfse?.Element(Ns + "valores")?.Element(Ns + "fed"), "pAliqEfetCBS")),
            ValorCbs = FormatMoney(vCbs),

            ValorServico = FormatMoney(vServ),
            ValorLiquido = FormatMoney(vLiq),
            ValorTotalNf = FormatMoney(vTotNf),
            TotalIbsCbs = FormatMoney(totalIbsCbs),

            ImovelMunicipio = T(imovel, "cMun"),
            ImovelEndereco = MontarEnderecoImovel(imovel),
            ImovelPercentualCopropriedade = FormatPct(T(imovel?.Element(Ns + "gLocacao"), "pCopropriedade")),
            ImovelValorOperacao = FormatMoney(T(imovel?.Element(Ns + "gLocacao"), "vTotOper")),
        };
    }

    private static ParteDanfse MontarParte(XElement? emit, XElement? parte, bool ehPrestador)
    {
        var fonteDoc = emit ?? parte;
        var endNac = emit?.Element(Ns + "enderNac")
                     ?? parte?.Element(Ns + "end")?.Element(Ns + "endNac");
        var end = parte?.Element(Ns + "end");
        var reg = parte?.Element(Ns + "regTrib");

        var cnpj = T(fonteDoc, "CNPJ");
        var cpf = T(fonteDoc, "CPF");
        var doc = FormatDoc(cnpj ?? cpf);

        var lgr = T(endNac, "xLgr") ?? T(end, "xLgr");
        var nro = T(endNac, "nro") ?? T(end, "nro");
        var bai = T(endNac, "xBairro") ?? T(end, "xBairro");
        var cpl = T(endNac, "xCpl") ?? T(end, "xCpl");
        var uf = T(endNac, "UF");
        var cMun = T(endNac, "cMun");
        var cep = SomenteDigitos(T(endNac, "CEP") ?? T(endNac is null ? end : null, "CEP") ?? T(end?.Element(Ns + "endNac"), "CEP"));

        // Prefer enderNac do emit; senão endereço do DPS.
        if (string.IsNullOrWhiteSpace(lgr) && end is not null)
        {
            lgr = T(end, "xLgr");
            nro = T(end, "nro");
            bai = T(end, "xBairro");
            cpl = T(end, "xCpl");
            cMun ??= T(end.Element(Ns + "endNac"), "cMun");
            cep ??= SomenteDigitos(T(end.Element(Ns + "endNac"), "CEP"));
        }

        var endereco = string.Join(", ", new[] { lgr, nro, cpl, bai }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var munUf = string.IsNullOrWhiteSpace(uf) ? (cMun ?? "") : $"{cMun} / {uf}";

        return new ParteDanfse
        {
            Documento = doc,
            Nome = T(fonteDoc, "xNome") ?? "—",
            InscricaoMunicipal = T(parte, "IM") ?? "",
            Telefone = T(parte, "fone") ?? "",
            Email = T(parte, "email") ?? "",
            Endereco = string.IsNullOrWhiteSpace(endereco) ? "—" : endereco,
            MunicipioUf = string.IsNullOrWhiteSpace(munUf) ? "—" : munUf,
            Cep = FormatCep(cep),
            CodigoMunicipioIbge = cMun ?? "",
            SimplesNacional = ehPrestador ? DescreverOpSimpNac(T(reg, "opSimpNac")) : "",
            RegimeApuracaoSn = ehPrestador ? DescreverRegApTribSn(T(reg, "regApTribSN")) : "",
        };
    }

    private static string? MontarEnderecoImovel(XElement? imovel)
    {
        var end = imovel?.Element(Ns + "gUnidImob")?.Element(Ns + "end");
        if (end is null) return null;
        return string.Join(", ", new[]
        {
            T(end, "xLgr"), T(end, "nro"), T(end, "xBairro"), FormatCep(SomenteDigitos(T(end, "CEP"))),
        }.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    private static string? T(XElement? parent, string name) =>
        parent?.Element(Ns + name)?.Value?.Trim();

    private static string Concat(params string?[] parts) =>
        string.Join(" — ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));

    private static string FormatDoc(string? digitos)
    {
        var d = SomenteDigitos(digitos);
        if (string.IsNullOrEmpty(d)) return "—";
        if (d.Length == 14)
            return $"{d[..2]}.{d[2..5]}.{d[5..8]}/{d[8..12]}-{d[12..]}";
        if (d.Length == 11)
            return $"{d[..3]}.{d[3..6]}.{d[6..9]}-{d[9..]}";
        return d;
    }

    private static string FormatCep(string? digitos)
    {
        var d = SomenteDigitos(digitos);
        if (string.IsNullOrEmpty(d)) return "";
        if (d.Length == 8) return $"{d[..5]}-{d[5..]}";
        return d;
    }

    private static string FormatMoney(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
            return raw;
        return v.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
    }

    private static string FormatPct(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        if (!decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
            return raw;
        return $"{v.ToString("0.##", CultureInfo.GetCultureInfo("pt-BR"))}%";
    }

    private static string FormatDateTime(string? iso)
    {
        if (string.IsNullOrWhiteSpace(iso)) return "—";
        if (DateTimeOffset.TryParse(iso, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dto))
            return dto.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.GetCultureInfo("pt-BR"));
        return iso;
    }

    private static string? SomenteDigitos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var d = new string(valor.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(d) ? null : d;
    }

    private static string DescreverTpEmit(string? c) => c switch
    {
        "1" => "Prestador",
        "2" => "Tomador",
        "3" => "Intermediário",
        _ => c ?? "—",
    };

    private static string DescreverCStat(string? c) => c switch
    {
        "100" => "NFS-e Gerada",
        "101" => "NFS-e de Decisão Judicial",
        "102" => "NFS-e de Decisão Administrativa",
        "103" => "NFS-e Gerada com Advertência",
        _ => string.IsNullOrWhiteSpace(c) ? "—" : c,
    };

    private static string DescreverFinNfse(string? c) => c switch
    {
        "0" => "NFS-e regular",
        "1" => "NFS-e de crédito",
        "2" => "NFS-e de débito",
        _ => c ?? "—",
    };

    private static string DescreverAmbGer(string? c) => c switch
    {
        "1" => "Prefeitura / Sefin Municipal",
        "2" => "Sefin Nacional",
        _ => c ?? "—",
    };

    private static string DescreverTribIssqn(string? c) => c switch
    {
        "1" => "Operação tributável",
        "2" => "Imunidade",
        "3" => "Exportação de serviço",
        "4" => "Não incidência",
        _ => c ?? "—",
    };

    private static string DescreverRetIssqn(string? c) => c switch
    {
        "1" => "Não retido",
        "2" => "Retido pelo tomador",
        "3" => "Retido pelo intermediário",
        _ => c ?? "",
    };

    private static string DescreverOpSimpNac(string? c) => c switch
    {
        "1" => "Não optante",
        "2" => "Optante — MEI",
        "3" => "Optante — ME/EPP",
        _ => c ?? "",
    };

    private static string DescreverRegApTribSn(string? c) => c switch
    {
        "1" => "Regime de apuração dos tributos federais e municipal pelo SN",
        "2" => "Regime de apuração dos tributos federais pelo SN e ISSQN pela legislação municipal",
        "3" => "Regime de apuração dos tributos federais e municipal pela legislação do SN / municipal",
        _ => c ?? "",
    };
}

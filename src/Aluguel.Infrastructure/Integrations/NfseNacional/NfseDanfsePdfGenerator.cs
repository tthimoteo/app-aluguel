using System.Globalization;
using System.Xml.Linq;
using Aluguel.Application.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>
/// Documento auxiliar da NFS-e em PDF (leitura a partir do XML SEFIN).
/// Layout pragmático alinhado à NT 008 (não é o leiaute oficial completo do DANFSe).
/// </summary>
public sealed class NfseDanfsePdfGenerator : INfsePdfGenerator
{
    static NfseDanfsePdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Gerar(byte[] xmlNfseAutorizado, string chaveAcesso, string competencia)
    {
        ArgumentNullException.ThrowIfNull(xmlNfseAutorizado);
        if (xmlNfseAutorizado.Length == 0)
            throw new ArgumentException("XML da NFS-e está vazio.", nameof(xmlNfseAutorizado));

        var dados = Extrair(xmlNfseAutorizado, chaveAcesso, competencia);
        var consultaUrl =
            $"https://www.nfse.gov.br/ConsultaPublica/?tpc=1&chave={dados.ChaveAcesso}";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("DANFSe").Bold().FontSize(14);
                            c.Item().Text("Documento Auxiliar da NFS-e").FontSize(10);
                            c.Item().Text($"Município emissor: {dados.LocalEmissao}").FontSize(8);
                            c.Item().Text($"Ambiente: {dados.Ambiente}").FontSize(8);
                        });
                        row.ConstantItem(120).AlignRight().Column(c =>
                        {
                            c.Item().AlignRight().Text($"Nº {dados.Numero}").Bold().FontSize(12);
                            c.Item().AlignRight().Text($"Série {dados.Serie}").FontSize(9);
                            c.Item().AlignRight().Text($"Competência {dados.Competencia}").FontSize(9);
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().Background(Colors.Grey.Lighten3).Padding(8).Column(c =>
                    {
                        c.Item().Text("Chave de acesso").SemiBold().FontSize(8);
                        c.Item().Text(dados.ChaveAcesso).FontFamily(Fonts.CourierNew).FontSize(8);
                        c.Item().PaddingTop(4).Text("Consulta pública / QR").SemiBold().FontSize(8);
                        c.Item().Text(consultaUrl).FontSize(7).FontColor(Colors.Blue.Darken2);
                    });

                    col.Item().Element(e => Secao(e, "Prestador / Emitente", [
                        ("Razão social", dados.EmitenteNome),
                        ("CNPJ/CPF", dados.EmitenteDoc),
                        ("Endereço", dados.EmitenteEndereco),
                    ]));

                    col.Item().Element(e => Secao(e, "Tomador", [
                        ("Nome", dados.TomadorNome),
                        ("CNPJ/CPF", dados.TomadorDoc),
                    ]));

                    col.Item().Element(e => Secao(e, "Serviço", [
                        ("Tributação nacional", dados.TributacaoNacional),
                        ("NBS", dados.Nbs),
                        ("Descrição", dados.DescricaoServico),
                        ("Local da prestação", dados.LocalPrestacao),
                    ]));

                    col.Item().Element(e => Secao(e, "Valores", [
                        ("Valor líquido", FormatarMoeda(dados.ValorLiquido)),
                        ("Data/hora processamento", dados.DataProcessamento),
                        ("Status", dados.Status),
                    ]));
                });

                page.Footer().AlignCenter().Text(txt =>
                {
                    txt.Span("Gerado pelo APP Aluguel Lucrare · ").FontSize(7).FontColor(Colors.Grey.Darken1);
                    txt.Span("autenticidade pela chave no portal nacional da NFS-e").FontSize(7).FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();
    }

    private static void Secao(IContainer container, string titulo, (string Rotulo, string Valor)[] linhas)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten1).Padding(8).Column(col =>
        {
            col.Item().Text(titulo).Bold().FontSize(10);
            col.Item().PaddingTop(4).Column(inner =>
            {
                foreach (var (rotulo, valor) in linhas)
                {
                    if (string.IsNullOrWhiteSpace(valor)) continue;
                    inner.Item().PaddingBottom(2).Row(r =>
                    {
                        r.ConstantItem(130).Text(rotulo).FontSize(8).FontColor(Colors.Grey.Darken2);
                        r.RelativeItem().Text(valor).FontSize(9);
                    });
                }
            });
        });
    }

    private static string FormatarMoeda(decimal? valor) =>
        valor is null
            ? ""
            : valor.Value.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));

    private static DadosDanfse Extrair(byte[] xmlBytes, string chaveFallback, string competenciaFallback)
    {
        using var stream = new MemoryStream(xmlBytes);
        var doc = XDocument.Load(stream);
        XNamespace ns = "http://www.sped.fazenda.gov.br/nfse";

        string? TAbs(string localName) =>
            doc.Descendants(ns + localName).Select(e => e.Value?.Trim()).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        var id = doc.Descendants(ns + "infNFSe").FirstOrDefault()?.Attribute("Id")?.Value;
        var chave = chaveFallback;
        if (!string.IsNullOrWhiteSpace(id) && id.StartsWith("NFS", StringComparison.OrdinalIgnoreCase) && id.Length > 3)
            chave = id[3..];

        var emit = doc.Descendants(ns + "emit").FirstOrDefault();
        var toma = doc.Descendants(ns + "toma").FirstOrDefault()
                   ?? doc.Descendants(ns + "DPS").Descendants(ns + "toma").FirstOrDefault();
        var ender = emit?.Element(ns + "enderNac");

        static string? Val(XElement? parent, XNamespace n, string name) =>
            parent?.Element(n + name)?.Value?.Trim()
            ?? parent?.Descendants(n + name).Select(e => e.Value?.Trim()).FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        var emitEnd = string.Join(", ", new[]
        {
            Val(ender, ns, "xLgr"),
            Val(ender, ns, "nro"),
            Val(ender, ns, "xBairro"),
            Val(ender, ns, "UF"),
            SomenteDigitos(Val(ender, ns, "CEP")),
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var ambGer = TAbs("ambGer");
        var ambiente = ambGer switch
        {
            "1" => "Produção",
            "2" => "Produção restrita",
            _ => ambGer ?? "—",
        };

        decimal? vLiq = null;
        if (decimal.TryParse(TAbs("vLiq"), NumberStyles.Number, CultureInfo.InvariantCulture, out var v))
            vLiq = v;

        var dCompet = TAbs("dCompet");
        var competencia = competenciaFallback;
        if (DateOnly.TryParse(dCompet, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dc))
            competencia = $"{dc.Month:00}/{dc.Year}";

        return new DadosDanfse(
            ChaveAcesso: chave,
            Numero: TAbs("nNFSe") ?? "—",
            Serie: "1",
            Competencia: competencia,
            LocalEmissao: TAbs("xLocEmi") ?? "—",
            LocalPrestacao: TAbs("xLocPrestacao") ?? TAbs("xLocEmi") ?? "—",
            Ambiente: ambiente,
            EmitenteNome: Val(emit, ns, "xNome") ?? "—",
            EmitenteDoc: FormatDoc(Val(emit, ns, "CNPJ") ?? Val(emit, ns, "CPF")),
            EmitenteEndereco: emitEnd,
            TomadorNome: Val(toma, ns, "xNome") ?? "—",
            TomadorDoc: FormatDoc(Val(toma, ns, "CNPJ") ?? Val(toma, ns, "CPF")),
            TributacaoNacional: TAbs("xTribNac") ?? "—",
            Nbs: TAbs("xNBS") ?? "—",
            DescricaoServico: TAbs("xDescServ") ?? "—",
            ValorLiquido: vLiq,
            DataProcessamento: TAbs("dhProc") ?? "—",
            Status: TAbs("cStat") == "100" ? "Autorizada (100)" : (TAbs("cStat") ?? "—"));
    }

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

    private static string? SomenteDigitos(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return null;
        var d = new string(valor.Where(char.IsDigit).ToArray());
        return string.IsNullOrEmpty(d) ? null : d;
    }

    private sealed record DadosDanfse(
        string ChaveAcesso,
        string Numero,
        string Serie,
        string Competencia,
        string LocalEmissao,
        string LocalPrestacao,
        string Ambiente,
        string EmitenteNome,
        string EmitenteDoc,
        string EmitenteEndereco,
        string TomadorNome,
        string TomadorDoc,
        string TributacaoNacional,
        string Nbs,
        string DescricaoServico,
        decimal? ValorLiquido,
        string DataProcessamento,
        string Status);
}

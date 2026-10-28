using System.Reflection;
using Aluguel.Application.Abstractions;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>
/// Gera o DANFSe (PDF) a partir do XML autorizado da NFS-e, conforme NT 008/2026 v1.02.
/// A API ADN de DANFSe foi desativada em 03/08/2026 — a renderização é local e usa
/// exclusivamente tags presentes no XML (item 2.1 da NT).
/// </summary>
public sealed class NfseDanfsePdfGenerator : INfsePdfGenerator
{
    private static readonly byte[]? LogoBytes = CarregarLogo();

    static NfseDanfsePdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Gerar(byte[] xmlNfseAutorizado, string? chaveAcesso = null, string? competencia = null)
    {
        ArgumentNullException.ThrowIfNull(xmlNfseAutorizado);
        if (xmlNfseAutorizado.Length == 0)
            throw new ArgumentException("XML da NFS-e autorizado está vazio.", nameof(xmlNfseAutorizado));

        var m = NfseDanfseXmlParser.Parse(xmlNfseAutorizado, chaveAcesso, competencia);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                // NT 008 §2.2.2: margens 0,15–0,20 cm; usamos 0,20 cm.
                page.MarginHorizontal(0.2f, Unit.Centimetre);
                page.MarginVertical(0.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(7).FontFamily(Fonts.Arial).FontColor(Colors.Black));

                page.Content().Border(1).BorderColor(Colors.Black).Padding(4).Column(root =>
                {
                    root.Spacing(3);
                    root.Item().Element(e => Cabecalho(e, m));
                    root.Item().Element(e => BlocoIdentificacao(e, m));
                    root.Item().Element(e => BlocoParte(e, "PRESTADOR / FORNECEDOR", m.Prestador, incluirRegime: true));
                    root.Item().Element(e => BlocoTomador(e, m));
                    root.Item().Element(e => BlocoDestinatario(e));
                    root.Item().Element(e => BlocoIntermediario(e));
                    root.Item().Element(e => BlocoServico(e, m));
                    root.Item().Element(e => BlocoIssqn(e, m));
                    root.Item().Element(e => BlocoIbsCbs(e, m));
                    root.Item().Element(e => BlocoTotais(e, m));
                    root.Item().Element(e => BlocoComplementares(e, m));
                });
            });
        }).GeneratePdf();
    }

    private static void Cabecalho(IContainer container, NfseDanfseModelo m)
    {
        container.Background(Colors.Grey.Lighten3).Border(0.5f).BorderColor(Colors.Black).Padding(4).Row(row =>
        {
            row.ConstantItem(90).AlignMiddle().Element(e =>
            {
                if (LogoBytes is { Length: > 0 })
                    e.Height(28).Image(LogoBytes).FitArea();
                else
                    e.AlignMiddle().Text("NFS-e").Bold().FontSize(11);
            });

            row.RelativeItem().AlignMiddle().AlignCenter().Column(c =>
            {
                c.Item().AlignCenter().Text("DANFSe v2.0").Bold().FontSize(9).FontFamily(Fonts.Arial);
                c.Item().AlignCenter().Text("Documento Auxiliar da NFS-e").Bold().FontSize(9).FontFamily(Fonts.Arial);
                if (m.Homologacao)
                {
                    c.Item().AlignCenter().Text("NFS-e SEM VALIDADE JURÍDICA")
                        .Bold().FontSize(9).FontColor(Colors.Red.Medium).FontFamily(Fonts.Arial);
                }
            });

            row.ConstantItem(130).Column(c =>
            {
                c.Item().AlignRight().Text($"Município: {m.LocalEmissao}").FontSize(8);
                c.Item().AlignRight().Text(m.AmbienteGerador).FontSize(6);
                c.Item().AlignRight().Text(m.TipoAmbiente).FontSize(6);
            });
        });
    }

    private static void BlocoIdentificacao(IContainer container, NfseDanfseModelo m)
    {
        container.Border(0.5f).BorderColor(Colors.Black).Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten3).Padding(2)
                .Text("DADOS DA NFS-e").Bold().FontSize(7);

            col.Item().Padding(3).Row(row =>
            {
                row.RelativeItem(3).Column(c =>
                {
                    Campo(c, "CHAVE DE ACESSO", m.ChaveAcesso, mono: true);
                    c.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().Element(e => CampoBox(e, "NÚMERO", m.NumeroNfse));
                        r.RelativeItem().Element(e => CampoBox(e, "COMPETÊNCIA", m.Competencia));
                        r.RelativeItem().Element(e => CampoBox(e, "DATA/HORA EMISSÃO NFS-e", m.DhProcessamento));
                    });
                    c.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().Element(e => CampoBox(e, "NÚMERO DPS", m.NumeroDps));
                        r.RelativeItem().Element(e => CampoBox(e, "SÉRIE DPS", m.SerieDps));
                        r.RelativeItem().Element(e => CampoBox(e, "DATA/HORA EMISSÃO DPS", m.DhEmissaoDps));
                    });
                    c.Item().PaddingTop(2).Row(r =>
                    {
                        r.RelativeItem().Background(Colors.Grey.Lighten3)
                            .Element(e => CampoBox(e, "EMITENTE DA NFS-e", m.EmitenteDescricao));
                        r.RelativeItem().Element(e => CampoBox(e, "SITUAÇÃO", m.SituacaoDescricao));
                        r.RelativeItem().Element(e => CampoBox(e, "FINALIDADE", m.FinalidadeDescricao));
                    });
                });

                row.ConstantItem(95).PaddingLeft(4).Column(qr =>
                {
                    qr.Item().AlignCenter().Width(54).Height(54).Image(GerarQrPng(m.ConsultaPublicaUrl)).FitArea();
                    qr.Item().PaddingTop(2).AlignCenter()
                        .Text("A autenticidade desta NFS-e pode ser verificada pela leitura deste código QR ou pela consulta da chave de acesso no portal nacional da NFS-e.")
                        .FontSize(5).AlignCenter();
                });
            });
        });
    }

    private static void BlocoParte(IContainer container, string titulo, ParteDanfse p, bool incluirRegime)
    {
        container.Border(0.5f).BorderColor(Colors.Black).Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten3).Padding(2).Text(titulo).Bold().FontSize(7);
            col.Item().Padding(3).Column(c =>
            {
                c.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => CampoBox(e, "CNPJ / CPF / NIF", p.Documento));
                    r.RelativeItem().Element(e => CampoBox(e, "Inscrição Municipal", string.IsNullOrWhiteSpace(p.InscricaoMunicipal) ? "—" : p.InscricaoMunicipal));
                    r.RelativeItem().Element(e => CampoBox(e, "Telefone", string.IsNullOrWhiteSpace(p.Telefone) ? "—" : p.Telefone));
                });
                Campo(c, "Nome / Nome Empresarial", p.Nome);
                c.Item().Row(r =>
                {
                    r.RelativeItem(2).Element(e => CampoBox(e, "Endereço", p.Endereco));
                    r.RelativeItem().Element(e => CampoBox(e, "Município / UF / IBGE", p.MunicipioUf));
                    r.RelativeItem().Element(e => CampoBox(e, "CEP", string.IsNullOrWhiteSpace(p.Cep) ? "—" : p.Cep));
                });
                if (!string.IsNullOrWhiteSpace(p.Email))
                    Campo(c, "E-mail", p.Email);
                if (incluirRegime)
                {
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Element(e => CampoBox(e, "Simples Nacional", string.IsNullOrWhiteSpace(p.SimplesNacional) ? "—" : p.SimplesNacional));
                        r.RelativeItem().Element(e => CampoBox(e, "Regime de Apuração SN", string.IsNullOrWhiteSpace(p.RegimeApuracaoSn) ? "—" : p.RegimeApuracaoSn));
                    });
                }
            });
        });
    }

    private static void BlocoTomador(IContainer container, NfseDanfseModelo m)
    {
        if (m.Tomador is null)
        {
            container.Border(0.5f).BorderColor(Colors.Black).Column(col =>
            {
                col.Item().Background(Colors.Grey.Lighten3).Padding(2)
                    .Text("TOMADOR / ADQUIRENTE DA OPERAÇÃO").Bold().FontSize(7);
                col.Item().Padding(6).Text("TOMADOR/ADQUIRENTE DA OPERAÇÃO NÃO IDENTIFICADO NA NFS-e").FontSize(7);
            });
            return;
        }

        BlocoParte(container, "TOMADOR / ADQUIRENTE DA OPERAÇÃO", m.Tomador, incluirRegime: false);
    }

    private static void BlocoDestinatario(IContainer container)
    {
        // NT 008 §2.3.2 — destinatário = tomador na locação típica.
        container.Border(0.5f).BorderColor(Colors.Black).Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten3).Padding(2)
                .Text("DESTINATÁRIO DA OPERAÇÃO").Bold().FontSize(7);
            col.Item().Padding(6)
                .Text("O DESTINATÁRIO É O PRÓPRIO TOMADOR/ADQUIRENTE DA OPERAÇÃO").FontSize(7);
        });
    }

    private static void BlocoIntermediario(IContainer container)
    {
        container.Border(0.5f).BorderColor(Colors.Black).Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten3).Padding(2)
                .Text("INTERMEDIÁRIO DA OPERAÇÃO").Bold().FontSize(7);
            col.Item().Padding(6)
                .Text("INTERMEDIÁRIO DA OPERAÇÃO NÃO IDENTIFICADO NA NFS-e").FontSize(7);
        });
    }

    private static void BlocoServico(IContainer container, NfseDanfseModelo m)
    {
        container.Border(0.5f).BorderColor(Colors.Black).Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten3).Padding(2)
                .Text("SERVIÇO PRESTADO").Bold().FontSize(7);
            col.Item().Padding(3).Column(c =>
            {
                c.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => CampoBox(e, "Cód. Tributação Nacional", m.TributacaoNacionalCodigo));
                    r.RelativeItem(2).Element(e => CampoBox(e, "Descrição Tributação Nacional", m.TributacaoNacionalDescricao));
                });
                c.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => CampoBox(e, "Código NBS", m.CodigoNbs));
                    r.RelativeItem(2).Element(e => CampoBox(e, "Descrição NBS", m.DescricaoNbs));
                });
                Campo(c, "Local da Prestação", m.LocalPrestacao);
                Campo(c, "Descrição do Serviço", m.DescricaoServico);
            });
        });
    }

    private static void BlocoIssqn(IContainer container, NfseDanfseModelo m)
    {
        container.Border(0.5f).BorderColor(Colors.Black).Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten3).Padding(2)
                .Text("TRIBUTAÇÃO MUNICIPAL (ISSQN)").Bold().FontSize(7);
            if (m.SemIssqn)
            {
                col.Item().Padding(6)
                    .Text($"TRIBUTAÇÃO MUNICIPAL (ISSQN) — OPERAÇÃO NÃO SUJEITA AO ISSQN ({m.TribIssqnDescricao})")
                    .FontSize(7);
                return;
            }

            col.Item().Padding(3).Row(r =>
            {
                r.RelativeItem().Element(e => CampoBox(e, "Tipo de Tributação", m.TribIssqnDescricao));
                r.RelativeItem().Element(e => CampoBox(e, "Retenção ISSQN", string.IsNullOrWhiteSpace(m.RetencaoIssqn) ? "—" : m.RetencaoIssqn));
            });
        });
    }

    private static void BlocoIbsCbs(IContainer container, NfseDanfseModelo m)
    {
        container.Border(0.5f).BorderColor(Colors.Black).Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten3).Padding(2)
                .Text("TRIBUTAÇÃO IBS / CBS").Bold().FontSize(7);
            col.Item().Padding(3).Column(c =>
            {
                c.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => CampoBox(e, "CST", string.IsNullOrWhiteSpace(m.Cst) ? "—" : m.Cst));
                    r.RelativeItem().Element(e => CampoBox(e, "cClassTrib", string.IsNullOrWhiteSpace(m.ClassificacaoTributaria) ? "—" : m.ClassificacaoTributaria));
                    r.RelativeItem().Element(e => CampoBox(e, "cIndOp", m.CodigoIndicadorOperacao));
                });
                Campo(c, "Localidade de Incidência IBS", string.IsNullOrWhiteSpace(m.LocalidadeIncidenciaIbs) ? "—" : m.LocalidadeIncidenciaIbs);
                Campo(c, "Base de Cálculo", string.IsNullOrWhiteSpace(m.BaseCalculoIbsCbs) ? "—" : m.BaseCalculoIbsCbs);
                c.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => CampoBox(e, "Alíq. IBS UF", OuTraco(m.AliquotaIbsUf)));
                    r.RelativeItem().Element(e => CampoBox(e, "Red. Alíq. UF", OuTraco(m.ReducaoAliqIbsUf)));
                    r.RelativeItem().Element(e => CampoBox(e, "Alíq. Efet. UF", OuTraco(m.AliquotaEfetivaIbsUf)));
                    r.RelativeItem().Element(e => CampoBox(e, "Valor IBS UF", OuTraco(m.ValorIbsUf)));
                });
                c.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => CampoBox(e, "Alíq. IBS Mun", OuTraco(m.AliquotaIbsMun)));
                    r.RelativeItem().Element(e => CampoBox(e, "Red. Alíq. Mun", OuTraco(m.ReducaoAliqIbsMun)));
                    r.RelativeItem().Element(e => CampoBox(e, "Alíq. Efet. Mun", OuTraco(m.AliquotaEfetivaIbsMun)));
                    r.RelativeItem().Element(e => CampoBox(e, "Valor IBS Mun", OuTraco(m.ValorIbsMun)));
                });
                c.Item().Row(r =>
                {
                    r.RelativeItem().Element(e => CampoBox(e, "Valor Total IBS", OuTraco(m.ValorTotalIbs)));
                    r.RelativeItem().Element(e => CampoBox(e, "Alíq. CBS", OuTraco(m.AliquotaCbs)));
                    r.RelativeItem().Element(e => CampoBox(e, "Red. Alíq. CBS", OuTraco(m.ReducaoAliqCbs)));
                    r.RelativeItem().Element(e => CampoBox(e, "Alíq. Efet. CBS", OuTraco(m.AliquotaEfetivaCbs)));
                    r.RelativeItem().Element(e => CampoBox(e, "Valor CBS", OuTraco(m.ValorCbs)));
                });
            });
        });
    }

    private static void BlocoTotais(IContainer container, NfseDanfseModelo m)
    {
        container.Border(0.5f).BorderColor(Colors.Black).Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten3).Padding(2)
                .Text("VALOR TOTAL DA NFS-e").Bold().FontSize(7);
            col.Item().Padding(3).Row(r =>
            {
                r.RelativeItem().Element(e => CampoBox(e, "Valor do Serviço", OuTraco(m.ValorServico)));
                r.RelativeItem().Element(e => CampoBox(e, "Valor Líquido", OuTraco(m.ValorLiquido)));
                r.RelativeItem().Element(e => CampoBox(e, "Total IBS/CBS", OuTraco(m.TotalIbsCbs)));
                r.RelativeItem().Background(Colors.Grey.Lighten3)
                    .Element(e => CampoBox(e, "Valor Líquido + IBS/CBS", OuTraco(m.ValorTotalNf)));
            });
        });
    }

    private static void BlocoComplementares(IContainer container, NfseDanfseModelo m)
    {
        container.Border(0.5f).BorderColor(Colors.Black).Column(col =>
        {
            col.Item().Background(Colors.Grey.Lighten3).Padding(2)
                .Text("INFORMAÇÕES COMPLEMENTARES").Bold().FontSize(7);
            col.Item().Padding(3).Column(c =>
            {
                if (!string.IsNullOrWhiteSpace(m.ImovelMunicipio) || !string.IsNullOrWhiteSpace(m.ImovelEndereco))
                {
                    Campo(c, "Imóvel — Município IBGE", OuTraco(m.ImovelMunicipio));
                    Campo(c, "Imóvel — Endereço", OuTraco(m.ImovelEndereco));
                    c.Item().Row(r =>
                    {
                        r.RelativeItem().Element(e => CampoBox(e, "Copropriedade", OuTraco(m.ImovelPercentualCopropriedade)));
                        r.RelativeItem().Element(e => CampoBox(e, "Valor Total da Operação", OuTraco(m.ImovelValorOperacao)));
                    });
                }

                if (!string.IsNullOrWhiteSpace(m.PercentualTotTribSn))
                {
                    Campo(c, "Totais Aproximados de Tributos (pTotTribSN — Lei 12.741/2012)",
                        $"{m.PercentualTotTribSn}%");
                }

                Campo(c, "Consulta pública", m.ConsultaPublicaUrl);
            });
        });
    }

    private static void Campo(ColumnDescriptor col, string label, string valor, bool mono = false)
    {
        col.Item().PaddingBottom(1).Column(c =>
        {
            c.Item().Text(label).Bold().FontSize(6).FontFamily(Fonts.Arial);
            var t = c.Item().Text(valor).FontSize(7);
            if (mono) t.FontFamily(Fonts.CourierNew);
        });
    }

    private static void CampoBox(IContainer container, string label, string valor)
    {
        container.Padding(1).Column(c =>
        {
            c.Item().Text(label).Bold().FontSize(6).FontFamily(Fonts.Arial);
            c.Item().Text(string.IsNullOrWhiteSpace(valor) ? "—" : valor).FontSize(7);
        });
    }

    private static string OuTraco(string? v) => string.IsNullOrWhiteSpace(v) ? "—" : v;

    private static byte[] GerarQrPng(string url)
    {
        using var gen = new QRCodeGenerator();
        using var data = gen.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        var qr = new PngByteQRCode(data);
        return qr.GetGraphic(4);
    }

    private static byte[]? CarregarLogo()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("Aluguel.Infrastructure.Nfse.logo-nfse.png");
        if (stream is null) return null;
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}

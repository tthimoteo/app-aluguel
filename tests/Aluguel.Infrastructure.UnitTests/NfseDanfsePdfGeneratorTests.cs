using Aluguel.Infrastructure.Integrations.NfseNacional;
using FluentAssertions;
using Xunit;

namespace Aluguel.Infrastructure.UnitTests;

public class NfseDanfsePdfGeneratorTests
{
    [Fact]
    public void Gerar_a_partir_do_xml_autorizado_sefin_produz_danfse_valido()
    {
        var xml = File.ReadAllBytes(FixturePath("nfse-autorizada-sefin.xml"));
        var gen = new NfseDanfsePdfGenerator();

        var pdf = gen.Gerar(xml);

        pdf.Should().NotBeEmpty();
        pdf.Length.Should().BeGreaterThan(5_000);
        System.Text.Encoding.ASCII.GetString(pdf.AsSpan(0, 5)).Should().Be("%PDF-");
        System.Text.Encoding.ASCII.GetString(pdf).Should().Contain("%%EOF");
    }

    [Fact]
    public void Parser_extrai_campos_obrigatorios_do_xml_autorizado()
    {
        var xml = File.ReadAllBytes(FixturePath("nfse-autorizada-sefin.xml"));
        var m = NfseDanfseXmlParser.Parse(xml, null, null);

        m.ChaveAcesso.Should().StartWith("3550308");
        m.ChaveAcesso.Should().HaveLength(50);
        m.NumeroNfse.Should().Be("1");
        m.Homologacao.Should().BeTrue();
        m.TributacaoNacionalCodigo.Should().Be("990301");
        m.CodigoNbs.Should().Be("110021000");
        m.Cst.Should().Be("200");
        m.ClassificacaoTributaria.Should().Be("200027");
        m.CodigoIndicadorOperacao.Should().Be("020201");
        m.SemIssqn.Should().BeTrue();
        m.Prestador.Documento.Should().Contain("15.809.930");
        m.Tomador.Should().NotBeNull();
        m.Tomador!.Nome.Should().Be("Thiago Thimoteo");
        m.ConsultaPublicaUrl.Should().Contain(m.ChaveAcesso);
        m.DescricaoServico.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Gerar_xml_vazio_lanca()
    {
        var gen = new NfseDanfsePdfGenerator();
        var act = () => gen.Gerar(Array.Empty<byte>());
        act.Should().Throw<ArgumentException>();
    }

    private static string FixturePath(string name)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", name);
        File.Exists(path).Should().BeTrue($"fixture {path} deve existir");
        return path;
    }
}

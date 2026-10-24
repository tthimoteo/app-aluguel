using Aluguel.Application.Abstractions;
using Aluguel.Domain.Fiscal;
using Aluguel.Infrastructure.Integrations.NfseNacional;
using FluentAssertions;
using Xunit;

namespace Aluguel.Infrastructure.UnitTests;

public class DpsXmlBuilderTests
{
    [Fact]
    public void MontarId_segue_layout_45_caracteres()
    {
        var id = DpsXmlBuilder.MontarId("3550308", "2", "15809930000168", "1", "100000000000001");
        id.Should().Be("DPS355030821580993000016800001100000000000001");
        id.Should().HaveLength(45);
    }

    [Fact]
    public void ResolverCodigosServico_codigo_longo_usa_padrao_lucrare()
    {
        var opts = new NfseOptions();
        var (nac, mun) = DpsXmlBuilder.ResolverCodigosServico("03158", opts);
        nac.Should().Be(NfseCodigosPadrao.CodigoTributacaoNacional);
        mun.Should().BeNull();
    }

    [Fact]
    public void ResolverCodigosServico_tres_digitos_vira_cTribMun()
    {
        var opts = new NfseOptions();
        var (nac, mun) = DpsXmlBuilder.ResolverCodigosServico("001", opts);
        nac.Should().Be(NfseCodigosPadrao.CodigoTributacaoNacional);
        mun.Should().Be("001");
    }

    [Fact]
    public void ResolverCodigosServico_seis_digitos_e_nacional()
    {
        var opts = new NfseOptions();
        var (nac, mun) = DpsXmlBuilder.ResolverCodigosServico(NfseCodigosPadrao.CodigoTributacaoNacional, opts);
        nac.Should().Be(NfseCodigosPadrao.CodigoTributacaoNacional);
        mun.Should().BeNull();
    }

    [Fact]
    public void Montar_gera_xml_com_codigos_padrao_lucrare_e_ibscbs()
    {
        var opts = new NfseOptions
        {
            Ambiente = "ProducaoRestrita",
            PercentualTotTribSn = 6.00m,
            IncluirIbsCbs = true,
            EnviarImPrestador = false,
        };

        var pedido = new NfseEmissaoPedido(
            Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            "10/2026",
            1500.50m,
            "Aluguel competência 10/2026",
            new NfsePartePrestador(
                "15809930000168", true, "45556059", "PRESTADOR TESTE",
                "11999999999", "prestador@teste.com",
                NfseCodigosPadrao.CodigoTributacaoNacional,
                NfseCodigosPadrao.CodigoIndicadorOperacao,
                NfseCodigosPadrao.ClassificacaoTributaria,
                "SimplesNacional",
                "3550308", "03159070", "Rua Faria Lobato", "137", null, "Vila Santa Clara"),
            new NfseParteTomador(
                "52998224725", false, "Tomador Teste", null,
                "11888888888", "toma@teste.com", "3550308",
                "01310100", "Av Paulista", "1000", null, "Bela Vista"),
            Array.Empty<byte>(),
            "senha",
            new NfseDadosImovel(
                CodigoMunicipioIbge: "3550308",
                Cep: "03159070",
                Logradouro: "Rua Faria Lobato",
                Numero: "137",
                Complemento: null,
                Bairro: "Vila Santa Clara",
                InscricaoImobiliaria: null,
                CodigoNbs: NfseCodigosPadrao.CodigoNbsResidencial));

        var xml = DpsXmlBuilder.Montar(pedido, opts, "100000000000001",
            new DateTimeOffset(2026, 10, 24, 15, 0, 0, TimeSpan.FromHours(-3)), out var idDps);

        idDps.Should().StartWith("DPS3550308");
        xml.Should().Contain("infDPS");
        xml.Should().Contain("finNFSe>0<");
        xml.Should().Contain("opSimpNac>3<");
        xml.Should().Contain($"cTribNac>{NfseCodigosPadrao.CodigoTributacaoNacional}<");
        xml.Should().Contain($"cIndOp>{NfseCodigosPadrao.CodigoIndicadorOperacao}<");
        xml.Should().Contain($"cClassTrib>{NfseCodigosPadrao.ClassificacaoTributaria}<");
        xml.Should().Contain($"cNBS>{NfseCodigosPadrao.CodigoNbsResidencial}<");
        xml.Should().Contain("IBSCBS");
        xml.Should().Contain("gLocacao");
        xml.Should().Contain("pCopropriedade>100.00<");
        xml.Should().Contain("vTotOper>1500.50<");
        xml.Should().NotContain("<IM>");
        xml.Should().NotContain("inscImobFisc");
        xml.Should().NotContain("<prest><CNPJ>15809930000168</CNPJ><xNome>");
        xml.Should().Contain($"CST>200<");
        xml.Should().Contain("tribISSQN>4<");
        xml.Should().Contain($"Id=\"{idDps}\"");
        xml.IndexOf("finNFSe", StringComparison.Ordinal).Should().BeLessThan(
            xml.IndexOf("cLocEmi", StringComparison.Ordinal));
        xml.IndexOf("cNBS>", StringComparison.Ordinal).Should().BeLessThan(
            xml.IndexOf("xDescServ>", StringComparison.Ordinal));
    }

    [Fact]
    public void GerarNumeroDps_sempre_15_digitos_sem_zero_inicial()
    {
        for (var i = 0; i < 50; i++)
        {
            var n = DpsXmlBuilder.GerarNumeroDps(Guid.NewGuid());
            n.Should().HaveLength(15);
            n[0].Should().NotBe('0');
            n.Should().MatchRegex("^[1-9][0-9]{14}$");
        }
    }

    [Fact]
    public async Task MockProvider_emite_com_sucesso()
    {
        var provider = new MockNfseNacionalProvider();
        var pedido = new NfseEmissaoPedido(
            Guid.NewGuid(), "11/2026", 100m, "Teste",
            new NfsePartePrestador(
                "15809930000168", true, "1", "P", null, null,
                NfseCodigosPadrao.CodigoTributacaoNacional,
                NfseCodigosPadrao.CodigoIndicadorOperacao,
                NfseCodigosPadrao.ClassificacaoTributaria,
                "SimplesNacional", "3550308", null, null, null, null, null),
            new NfseParteTomador("52998224725", false, "T", null, null, null, null, null, null, null, null, null),
            Array.Empty<byte>(), "x");

        var r = await provider.EmitirAsync(pedido);
        r.Sucesso.Should().BeTrue();
        r.XmlNfse.Should().NotBeNullOrEmpty();
        r.ChaveAcesso.Should().NotBeNullOrWhiteSpace();
    }
}

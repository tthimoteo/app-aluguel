using Aluguel.Infrastructure.Integrations.NfseNacional;
using FluentAssertions;
using Xunit;

namespace Aluguel.Infrastructure.UnitTests;

public class NfseDanfsePdfGeneratorTests
{
    [Fact]
    public void Gerar_produz_pdf_valido_com_chave()
    {
        var xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <NFSe versao="1.01" xmlns="http://www.sped.fazenda.gov.br/nfse">
              <infNFSe Id="NFS35503082215809930000168000000000000226108863589574">
                <xLocEmi>São Paulo</xLocEmi>
                <xLocPrestacao>São Paulo</xLocPrestacao>
                <nNFSe>2</nNFSe>
                <xTribNac>Locação de Bens Imóveis</xTribNac>
                <xNBS>Locação de imóveis residenciais</xNBS>
                <ambGer>2</ambGer>
                <cStat>100</cStat>
                <dhProc>2026-10-24T11:57:00-03:00</dhProc>
                <emit>
                  <CNPJ>15809930000168</CNPJ>
                  <xNome>PRESTADOR TESTE LTDA</xNome>
                  <enderNac>
                    <xLgr>Rua A</xLgr><nro>1</nro><xBairro>Centro</xBairro><UF>SP</UF><CEP>01001000</CEP>
                  </enderNac>
                </emit>
                <valores><vLiq>100.00</vLiq></valores>
                <DPS>
                  <infDPS>
                    <dCompet>2026-07-01</dCompet>
                    <toma><CPF>28958973870</CPF><xNome>Tomador Teste</xNome></toma>
                    <serv><cServ><xDescServ>Aluguel competência 07/2026</xDescServ></cServ></serv>
                  </infDPS>
                </DPS>
              </infNFSe>
            </NFSe>
            """;

        var gen = new NfseDanfsePdfGenerator();
        var pdf = gen.Gerar(
            System.Text.Encoding.UTF8.GetBytes(xml),
            "35503082215809930000168000000000000226108863589574",
            "07/2026");

        pdf.Should().NotBeEmpty();
        pdf.Length.Should().BeGreaterThan(1000);
        System.Text.Encoding.ASCII.GetString(pdf.AsSpan(0, 5)).Should().Be("%PDF-");
        System.Text.Encoding.ASCII.GetString(pdf).Should().Contain("%%EOF");
    }
}

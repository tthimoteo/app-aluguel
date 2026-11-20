using System.Globalization;
using System.Text;
using Aluguel.Application.Abstractions;

namespace Aluguel.Infrastructure.Integrations.NfseNacional;

/// <summary>Emissão/cancelamento local simulados (CI / Ambient=Mock) — não chama a SEFIN.</summary>
public sealed class MockNfseNacionalProvider : INfseNacionalProvider
{
    public Task<NfseEmissaoResultado> EmitirAsync(NfseEmissaoPedido pedido, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(pedido);

        var numero = Math.Abs(BitConverter.ToInt64(pedido.NfseId.ToByteArray(), 0) % 900_000) + 100_000;
        // Chave só dígitos para o DANFSe/consulta pública.
        var chave = $"355030822{numero:D20}{DateTime.UtcNow:yyMMddHHmm}";
        if (chave.Length > 50) chave = chave[..50];

        var competencia = pedido.Competencia;
        var dCompet = "2026-11-01";
        if (competencia.Length >= 7
            && int.TryParse(competencia.AsSpan(0, 2), out var mes)
            && int.TryParse(competencia.AsSpan(3, 4), out var ano))
            dCompet = $"{ano:0000}-{mes:00}-01";

        var valor = pedido.ValorServico.ToString("F2", CultureInfo.InvariantCulture);
        var docPrest = new string((pedido.Prestador.Documento ?? "").Where(char.IsDigit).ToArray());
        var nomePrest = Escape(pedido.Prestador.RazaoSocial ?? "Prestador Mock");
        var docToma = new string((pedido.Tomador.Documento ?? "").Where(char.IsDigit).ToArray());
        var nomeToma = Escape(pedido.Tomador.Nome ?? "Tomador Mock");
        var desc = Escape(pedido.DescricaoServico ?? "Serviço de locação");
        var agora = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz", CultureInfo.InvariantCulture);
        var idInf = $"NFS{chave}";
        var idDps = $"DPS{numero}";

        // XML mínimo compatível com NfseDanfseXmlParser (namespace SEFIN + infNFSe/DPS).
        var xml = Encoding.UTF8.GetBytes(
            $"""
            <?xml version="1.0" encoding="utf-8"?>
            <NFSe versao="1.01" xmlns="http://www.sped.fazenda.gov.br/nfse">
              <infNFSe Id="{idInf}">
                <xLocEmi>São Paulo</xLocEmi>
                <xLocPrestacao>São Paulo</xLocPrestacao>
                <nNFSe>{numero}</nNFSe>
                <xTribNac>Locação de Bens Imóveis</xTribNac>
                <xNBS>Locação de imóveis residenciais</xNBS>
                <verAplic>LucrareAluguelMock</verAplic>
                <ambGer>2</ambGer>
                <tpEmis>1</tpEmis>
                <procEmi>1</procEmi>
                <cStat>100</cStat>
                <dhProc>{agora}</dhProc>
                <emit>
                  <CNPJ>{docPrest}</CNPJ>
                  <xNome>{nomePrest}</xNome>
                  <enderNac>
                    <xLgr>Rua Mock</xLgr><nro>1</nro><xBairro>Centro</xBairro>
                    <cMun>3550308</cMun><UF>SP</UF><CEP>01001000</CEP>
                  </enderNac>
                </emit>
                <valores><vLiq>{valor}</vLiq></valores>
                <IBSCBS>
                  <cLocalidadeIncid>3550308</cLocalidadeIncid>
                  <xLocalidadeIncid>São Paulo</xLocalidadeIncid>
                  <totCIBS>
                    <vTotNF>{valor}</vTotNF>
                    <gIBS><vIBSTot>0.00</vIBSTot></gIBS>
                    <gCBS><vCBS>0.00</vCBS></gCBS>
                  </totCIBS>
                </IBSCBS>
                <DPS versao="1.01">
                  <infDPS Id="{idDps}">
                    <tpAmb>2</tpAmb>
                    <dhEmi>{agora}</dhEmi>
                    <verAplic>LucrareAluguelMock</verAplic>
                    <serie>1</serie>
                    <nDPS>{numero}</nDPS>
                    <dCompet>{dCompet}</dCompet>
                    <tpEmit>1</tpEmit>
                    <finNFSe>0</finNFSe>
                    <cLocEmi>3550308</cLocEmi>
                    <prest><CNPJ>{docPrest}</CNPJ></prest>
                    <toma><CPF>{docToma}</CPF><xNome>{nomeToma}</xNome></toma>
                    <serv>
                      <locPrest><cLocPrestacao>3550308</cLocPrestacao></locPrest>
                      <cServ>
                        <cTribNac>990301</cTribNac>
                        <cNBS>110021000</cNBS>
                        <xDescServ>{desc}</xDescServ>
                      </cServ>
                    </serv>
                    <valores>
                      <vServPrest><vServ>{valor}</vServ></vServPrest>
                      <trib><tribMun><tribISSQN>4</tribISSQN><tpRetISSQN>1</tpRetISSQN></tribMun>
                        <totTrib><pTotTribSN>6.00</pTotTribSN></totTrib></trib>
                    </valores>
                    <IBSCBS>
                      <cIndOp>020201</cIndOp>
                      <valores><trib><CST>200</CST><cClassTrib>200027</cClassTrib></trib></valores>
                    </IBSCBS>
                  </infDPS>
                </DPS>
              </infNFSe>
            </NFSe>
            """);

        return Task.FromResult(new NfseEmissaoResultado(
            Sucesso: true,
            Numero: numero,
            Serie: "1",
            ChaveAcesso: chave,
            IdDps: idDps,
            XmlNfse: xml,
            XmlDpsEnviado: null,
            MensagemErro: null));
    }

    public Task<NfseCancelamentoResultado> CancelarAsync(
        NfseCancelamentoPedido pedido, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(pedido);

        var chave = new string((pedido.ChaveAcesso ?? "").Where(char.IsDigit).ToArray());
        var prot = $"MOCK-CANC-{chave[^Math.Min(8, Math.Max(chave.Length, 1))..]}";
        var xml = Encoding.UTF8.GetBytes(
            $"""<?xml version="1.0" encoding="UTF-8"?><evento xmlns="http://www.sped.fazenda.gov.br/nfse"><infEvento><chNFSe>{pedido.ChaveAcesso}</chNFSe><nProt>{prot}</nProt><tpEvento>e101101</tpEvento></infEvento></evento>""");

        return Task.FromResult(new NfseCancelamentoResultado(
            Sucesso: true,
            Protocolo: prot,
            XmlEvento: xml,
            MensagemErro: null));
    }

    private static string Escape(string s) =>
        System.Security.SecurityElement.Escape(s) ?? s;
}

using System.Text;
using Aluguel.Application.Abstractions;
using Aluguel.Infrastructure.Integrations.NfseNacional;
using Xunit;

namespace Aluguel.Infrastructure.UnitTests;

public class MockNfseDanfseCompatTests
{
    [Fact]
    public async Task EmitirAsync_XmlCompativelComDanfse()
    {
        var provider = new MockNfseNacionalProvider();
        var pedido = new NfseEmissaoPedido(
            Guid.NewGuid(),
            "11/2026",
            2500m,
            "Aluguel competência 11/2026",
            new NfsePartePrestador(
                "15809930000168", true, "123", "Prestador", null, null,
                "990301", "020201", "200027", "SimplesNacional", "3550308",
                null, null, null, null, null),
            new NfseParteTomador(
                "28958973870", false, "Tomador", null, null, null,
                "3550308", null, null, null, null, null),
            CertificadoPfx: [],
            SenhaCertificado: "");

        var r = await provider.EmitirAsync(pedido);
        Assert.True(r.Sucesso);
        Assert.NotNull(r.XmlNfse);
        var pdf = new NfseDanfsePdfGenerator().Gerar(r.XmlNfse!, r.ChaveAcesso, "11/2026");
        Assert.True(pdf.Length > 1000);
        Assert.Equal("%PDF", Encoding.ASCII.GetString(pdf.AsSpan(0, 4)));
    }
}

using Aluguel.Application.Abstractions;
using Aluguel.Infrastructure.Integrations.NfseNacional;
using FluentAssertions;
using Xunit;

namespace Aluguel.Infrastructure.UnitTests;

public class EventoCancelamentoXmlBuilderTests
{
    private const string Chave50 = "35503082215809930000168000000000000126108889952559";

    [Fact]
    public void MontarIdPedido_segue_padrao_PRE_chave_101_seq()
    {
        var id = EventoCancelamentoXmlBuilder.MontarIdPedido(Chave50, 1);
        id.Should().Be($"PRE{Chave50}101001");
        id.Should().HaveLength(59);
    }

    [Fact]
    public void Montar_gera_pedRegEvento_e101101()
    {
        var opts = new NfseOptions { Ambiente = "ProducaoRestrita", VerAplic = "LucrareAluguel1.0" };
        var pedido = new NfseCancelamentoPedido(
            Chave50, "15809930000168", true,
            "Remocao de cobranca no APP Aluguel - cancelamento solicitado pelo emitente.",
            "1", [1], "x");

        var xml = EventoCancelamentoXmlBuilder.Montar(
            pedido, opts, DateTimeOffset.Parse("2026-10-28T15:00:00-03:00"), out var id);

        id.Should().StartWith("PRE");
        xml.Should().Contain("pedRegEvento");
        xml.Should().Contain("e101101");
        xml.Should().Contain("<xDesc>Cancelamento de NFS-e</xDesc>");
        xml.Should().Contain($"<chNFSe>{Chave50}</chNFSe>");
        xml.Should().Contain("<CNPJAutor>15809930000168</CNPJAutor>");
        xml.Should().Contain($"Id=\"{id}\"");
    }

    [Fact]
    public void Montar_sanitiza_travessao_unicode_em_xMotivo()
    {
        var opts = new NfseOptions { Ambiente = "ProducaoRestrita" };
        var pedido = new NfseCancelamentoPedido(
            Chave50, "15809930000168", true,
            "Remoção de cobrança — cancelamento solicitado pelo emitente.",
            "1", [1], "x");

        var xml = EventoCancelamentoXmlBuilder.Montar(
            pedido, opts, DateTimeOffset.UtcNow, out _);

        xml.Should().NotContain("\u2014");
        xml.Should().Contain("<xMotivo>");
        xml.Should().Contain("cancelamento solicitado pelo emitente.");
    }
}

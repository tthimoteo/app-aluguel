using Aluguel.Domain.Financeiro;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class IntegracaoBoletoClienteTests
{
    [Fact]
    public void Criar_normaliza_provedor_e_persiste_json_publico()
    {
        var integ = new IntegracaoBoletoCliente(
            Guid.NewGuid(),
            Guid.NewGuid(),
            " ITAU ",
            """{"agencia":"1234","conta":"56789"}""",
            [1, 2, 3, 4]);

        integ.ProvedorCodigo.Should().Be("itau");
        integ.Ativo.Should().BeTrue();
        integ.LerConfiguracaoPublica()["agencia"].Should().Be("1234");
    }

    [Fact]
    public void Atualizar_troca_provedor_e_config()
    {
        var integ = new IntegracaoBoletoCliente(
            Guid.NewGuid(), Guid.NewGuid(), "bradesco", "{}", [9]);

        integ.Atualizar("santander", """{"workspace_id":"ws-1"}""", [7, 8]);
        integ.ProvedorCodigo.Should().Be("santander");
        integ.LerConfiguracaoPublica()["workspace_id"].Should().Be("ws-1");
        integ.SegredosCifrados.Should().Equal(7, 8);
    }

    [Fact]
    public void Boleto_guarda_provedor_e_id_externo()
    {
        var b = new Boleto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "10/2026", 100m,
            new DateOnly(2026, 11, 10),
            nossoNumero: "001123",
            linhaDigitavel: "00190...",
            provedorCodigo: "banco_do_brasil",
            idExterno: "ext-99");

        b.ProvedorCodigo.Should().Be("banco_do_brasil");
        b.IdExterno.Should().Be("ext-99");
    }
}

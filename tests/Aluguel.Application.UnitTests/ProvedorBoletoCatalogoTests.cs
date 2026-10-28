using Aluguel.Application.Boletos;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class ProvedorBoletoCatalogoTests
{
    [Fact]
    public void Catalogo_inclui_os_dez_provedores_iniciais()
    {
        var codigos = ProvedorBoletoCatalogo.Todos.Select(p => p.Codigo).ToList();
        codigos.Should().Contain([
            "itau", "bradesco", "banco_do_brasil", "santander", "caixa",
            "sicredi", "mercado_pago", "inter", "c6", "nubank",
        ]);
        ProvedorBoletoCatalogo.Todos.Should().HaveCountGreaterThanOrEqualTo(10);
    }

    [Theory]
    [InlineData("itau", "341")]
    [InlineData("bradesco", "237")]
    [InlineData("banco_do_brasil", "001")]
    [InlineData("mercado_pago", "")]
    public void Obter_retorna_codigo_febraban(string codigo, string banco)
    {
        var def = ProvedorBoletoCatalogo.Obter(codigo);
        def.Should().NotBeNull();
        def!.CodigoBanco.Should().Be(banco);
        def.Campos.Should().NotBeEmpty();
        def.Campos.Should().Contain(c => c.Tipo == TipoCampoProvedorBoleto.Segredo);
    }

    [Fact]
    public void Campos_obrigatorios_tem_rotulo()
    {
        foreach (var p in ProvedorBoletoCatalogo.Todos)
        {
            p.Campos.Where(c => c.Obrigatorio).Should().NotBeEmpty($"provedor {p.Codigo}");
            foreach (var c in p.Campos)
                c.Rotulo.Should().NotBeNullOrWhiteSpace();
        }
    }
}

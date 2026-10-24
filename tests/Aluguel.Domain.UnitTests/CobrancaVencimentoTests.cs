using Aluguel.Domain.Financeiro;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class CobrancaVencimentoTests
{
    [Theory]
    [InlineData("10/2026", 15, 2026, 11, 15)]
    [InlineData("11/2026", 10, 2026, 12, 10)]
    [InlineData("12/2026", 31, 2027, 1, 31)]
    [InlineData("01/2026", 31, 2026, 2, 28)] // clamp fevereiro não-bissexto
    public void Calcular_usa_mes_seguinte_a_competencia(
        string competencia, int dia, int ano, int mes, int diaEsperado)
    {
        var venc = CobrancaVencimento.Calcular(competencia, dia);
        venc.Should().Be(new DateOnly(ano, mes, diaEsperado));
    }
}

using Aluguel.Application.Cobrancas;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class CobrancaVencimentoTests
{
    [Theory]
    [InlineData("10/2026", 15, 2026, 11, 15)]
    [InlineData("01/2026", 10, 2026, 2, 10)]
    [InlineData("12/2026", 15, 2027, 1, 15)]
    [InlineData("09/2026", 1, 2026, 10, 1)]
    public void Calcular_usa_dia_no_mes_seguinte_a_competencia(
        string competencia, int dia, int anoEsperado, int mesEsperado, int diaEsperado)
    {
        var venc = CobrancaVencimento.Calcular(competencia, dia);
        venc.Should().Be(new DateOnly(anoEsperado, mesEsperado, diaEsperado));
    }

    [Fact]
    public void Calcular_ajusta_dia_quando_mes_seguinte_tem_menos_dias()
    {
        // Competência 01/2026 → vencimento em fevereiro; dia 31 vira 28/02/2026 (2026 não é bissexto).
        var venc = CobrancaVencimento.Calcular("01/2026", 31);
        venc.Should().Be(new DateOnly(2026, 2, 28));
    }
}

using Aluguel.Domain.Financeiro;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class BoletoTests
{
    [Fact]
    public void MarcarPago_atualiza_status()
    {
        var boleto = new Boleto(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "10/2026", 1000m,
            new DateOnly(2026, 10, 10));

        boleto.Status.Should().Be(StatusBoleto.Gerado);
        boleto.MarcarPago();
        boleto.Status.Should().Be(StatusBoleto.Pago);
    }
}

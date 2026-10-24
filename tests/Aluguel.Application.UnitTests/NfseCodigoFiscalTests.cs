using Aluguel.Application.Common.Validacoes;
using Aluguel.Domain.Fiscal;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class NfseCodigoFiscalTests
{
    [Theory]
    [InlineData("990301", true)]
    [InlineData("990.301", true)]
    [InlineData("03158", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EhCodigoTributacaoNacional(string? valor, bool ok) =>
        NfseCodigoFiscal.EhCodigoTributacaoNacional(valor).Should().Be(ok);

    [Fact]
    public void Normalizar_padroes_lucrare()
    {
        NfseCodigoFiscal.NormalizarDigitos(NfseCodigosPadrao.CodigoTributacaoNacional, 6)
            .Should().Be("990301");
        NfseCodigoFiscal.NormalizarDigitos(NfseCodigosPadrao.CodigoIndicadorOperacao, 6)
            .Should().Be("020201");
        NfseCodigoFiscal.NormalizarDigitos(NfseCodigosPadrao.ClassificacaoTributaria, 6)
            .Should().Be("200027");
    }
}

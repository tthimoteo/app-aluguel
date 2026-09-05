using Aluguel.Application.Common.Validacoes;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class DocumentoTests
{
    [Theory]
    [InlineData("39053344705", true)]
    [InlineData("52998224725", true)]
    [InlineData("11111111111", false)]  // repetidos
    [InlineData("39053344700", false)]  // dígito inválido
    [InlineData("123", false)]          // tamanho inválido
    [InlineData("", false)]
    public void CpfValido_avalia_digitos_verificadores(string cpf, bool esperado) =>
        Documento.CpfValido(cpf).Should().Be(esperado);

    [Theory]
    [InlineData("11222333000181", true)]
    [InlineData("04252011000110", true)]
    [InlineData("11222333000100", false)] // dígito inválido
    [InlineData("00000000000000", false)] // repetidos
    [InlineData("123", false)]
    public void CnpjValido_avalia_digitos_verificadores(string cnpj, bool esperado) =>
        Documento.CnpjValido(cnpj).Should().Be(esperado);

    [Fact]
    public void SomenteDigitos_remove_mascara() =>
        Documento.SomenteDigitos("390.533.447-05").Should().Be("39053344705");
}

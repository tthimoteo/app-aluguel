using Aluguel.Domain.Fiscal;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class CertificadoDigitalTests
{
    [Fact]
    public void Novo_certificado_nasce_ativo()
    {
        var c = new CertificadoDigital(
            Guid.NewGuid(), Guid.NewGuid(), "certificates/t/c/a.pfx",
            "ABC123", DateTimeOffset.UtcNow.AddYears(1), [1, 2, 3]);

        c.Ativo.Should().BeTrue();
        c.Thumbprint.Should().Be("ABC123");
    }

    [Fact]
    public void Desativar_marca_como_inativo()
    {
        var c = new CertificadoDigital(
            Guid.NewGuid(), Guid.NewGuid(), "certificates/t/c/a.pfx",
            "ABC123", DateTimeOffset.UtcNow.AddYears(1), [1, 2, 3]);

        c.Desativar();

        c.Ativo.Should().BeFalse();
    }
}

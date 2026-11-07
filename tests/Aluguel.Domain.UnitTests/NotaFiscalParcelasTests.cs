using Aluguel.Domain.Fiscal;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class NotaFiscalParcelasTests
{
    [Fact]
    public void Construtor_guarda_parcelas_de_aluguel_iptu_e_condominio()
    {
        var nfse = new NotaFiscalServico(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "10/2026", 3500m,
            valorAluguel: 2500m, valorIptu: 400m, valorCondominio: 600m);

        nfse.ValorServico.Should().Be(3500m);
        nfse.ValorAluguel.Should().Be(2500m);
        nfse.ValorIptu.Should().Be(400m);
        nfse.ValorCondominio.Should().Be(600m);
    }

    [Fact]
    public void Sem_parcelas_explicitas_aluguel_igual_ao_servico()
    {
        var nfse = new NotaFiscalServico(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "10/2026", 2000m);

        nfse.ValorAluguel.Should().Be(2000m);
        nfse.ValorIptu.Should().Be(0m);
        nfse.ValorCondominio.Should().Be(0m);
    }
}

using Aluguel.Domain.Fiscal;
using FluentAssertions;
using Xunit;

namespace Aluguel.Domain.UnitTests;

public class NotaFiscalCancelamentoTests
{
    [Fact]
    public void MarcarCancelada_de_emitida_preenche_motivo_e_protocolo()
    {
        var nfse = new NotaFiscalServico(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "10/2026", 100m);
        var em = DateTimeOffset.Parse("2026-10-28T12:00:00Z");
        nfse.MarcarEmitida(1, "1", "CHAVE", Guid.NewGuid(), em);

        var canc = DateTimeOffset.Parse("2026-10-28T14:00:00Z");
        nfse.MarcarCancelada("Motivo teste cancelamento NFS-e", "PROT-1", canc);

        nfse.Status.Should().Be(StatusNfse.Cancelada);
        nfse.MotivoCancelamento.Should().Be("Motivo teste cancelamento NFS-e");
        nfse.ProtocoloCancelamento.Should().Be("PROT-1");
        nfse.DataCancelamento.Should().Be(canc);
    }

    [Fact]
    public void MarcarCancelada_de_rascunho_lanca()
    {
        var nfse = new NotaFiscalServico(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "10/2026", 100m);
        var act = () => nfse.MarcarCancelada("Motivo suficiente aqui.", null, DateTimeOffset.UtcNow);
        act.Should().Throw<InvalidOperationException>();
    }
}

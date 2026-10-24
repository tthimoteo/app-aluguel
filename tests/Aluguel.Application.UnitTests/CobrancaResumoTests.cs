using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Financeiro;
using Aluguel.Domain.Fiscal;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class CobrancaResumoTests
{
    private static readonly Guid Tenant = Guid.Parse("2e8c8210-56a6-4078-9cde-f61fd6142ae8");
    private static readonly Guid ClienteId = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");
    private static readonly Guid ImovelId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    [Fact]
    public async Task Contar_separa_em_aberto_e_atrasadas()
    {
        var repo = new FakeCobrancaRepository();
        var hoje = new DateOnly(2026, 10, 24);

        // Em aberto, ainda no prazo
        var n1 = new NotaFiscalServico(Tenant, ClienteId, ImovelId, "09/2026", 1000m);
        n1.MarcarEmitida(1, "1", "C1", Guid.NewGuid(), DateTimeOffset.UtcNow);
        repo.AdicionarNfse(n1);
        repo.AdicionarBoleto(new Boleto(Tenant, ClienteId, ImovelId, "09/2026", 1000m,
            new DateOnly(2026, 10, 30), n1.Id));

        // Em aberto e atrasada
        var n2 = new NotaFiscalServico(Tenant, ClienteId, ImovelId, "08/2026", 1000m);
        n2.MarcarEmitida(2, "1", "C2", Guid.NewGuid(), DateTimeOffset.UtcNow);
        repo.AdicionarNfse(n2);
        repo.AdicionarBoleto(new Boleto(Tenant, ClienteId, ImovelId, "08/2026", 1000m,
            new DateOnly(2026, 9, 15), n2.Id));

        // Paga — não conta
        var n3 = new NotaFiscalServico(Tenant, ClienteId, ImovelId, "07/2026", 1000m);
        n3.MarcarEmitida(3, "1", "C3", Guid.NewGuid(), DateTimeOffset.UtcNow);
        repo.AdicionarNfse(n3);
        repo.AdicionarBoleto(new Boleto(Tenant, ClienteId, ImovelId, "07/2026", 1000m,
            new DateOnly(2026, 8, 10), n3.Id));
        repo.AdicionarPagamento(new Pagamento(Tenant, ClienteId, ImovelId, "07/2026", 1000m,
            new DateOnly(2026, 8, 5), nfseId: n3.Id));

        var resumo = await repo.ContarResumoVencimentosAsync(ClienteId, hoje);

        resumo.EmAberto.Should().Be(2);
        resumo.Atrasadas.Should().Be(1);
    }
}

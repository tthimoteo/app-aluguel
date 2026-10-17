using Aluguel.Application.Cobrancas.RegistrarPagamentoCobranca;
using Aluguel.Application.Cobrancas.RemoverCobranca;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Financeiro;
using Aluguel.Domain.Fiscal;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class CobrancaPagamentoRemocaoTests
{
    private static readonly Guid Tenant = Guid.Parse("2e8c8210-56a6-4078-9cde-f61fd6142ae8");
    private static readonly Guid ClienteId = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");
    private static readonly Guid ImovelId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private sealed record Cenario(
        FakeCobrancaRepository Cobrancas,
        FakeClienteRepository Clientes,
        FakePlanoRepository Planos,
        FakeCurrentUser User,
        NotaFiscalServico Nfse,
        Boleto Boleto);

    private static Cenario Montar(bool comPagamento = false)
    {
        var plano = new Plano(Guid.NewGuid(), PlanoCodigo.Avancado, "Avançado", 10, 5, true, 0, 249.99m);
        var cliente = Cliente.CriarPessoaFisica(Tenant, plano.Id, "Locador", "39053344705", null, null, null, null);

        var clientes = new FakeClienteRepository();
        clientes.Adicionar(cliente);
        var planos = new FakePlanoRepository();
        planos.Itens.Add(plano);

        var cobrancas = new FakeCobrancaRepository();
        // Admin ignora escopo/plano; ClienteId da NFS-e não precisa coincidir com o do FakeCliente.
        var nfse = new NotaFiscalServico(Tenant, ClienteId, ImovelId, "10/2026", 2500m);
        nfse.MarcarEmitida(1, "1", "CHAVE", Guid.NewGuid(), DateTimeOffset.UtcNow);
        cobrancas.AdicionarNfse(nfse);

        var boleto = new Boleto(Tenant, ClienteId, ImovelId, "10/2026", 2500m,
            new DateOnly(2026, 10, 10), nfse.Id, "BOL1", "23790");
        cobrancas.AdicionarBoleto(boleto);

        if (comPagamento)
        {
            cobrancas.AdicionarPagamento(new Pagamento(Tenant, ClienteId, ImovelId, "10/2026", 2500m,
                new DateOnly(2026, 10, 12), nfseId: nfse.Id));
            boleto.MarcarPago();
        }

        var user = new FakeCurrentUser(Guid.NewGuid(), clienteId: null, ehAdministrador: true);
        return new Cenario(cobrancas, clientes, planos, user, nfse, boleto);
    }

    [Fact]
    public async Task Registrar_pagamento_cria_pagamento_e_marca_boleto()
    {
        var c = Montar();
        var handler = new RegistrarPagamentoCobrancaCommandHandler(
            c.Cobrancas, c.Clientes, c.Planos, c.User);

        var dto = await handler.Handle(
            new RegistrarPagamentoCobrancaCommand(c.Nfse.Id, new DateOnly(2026, 10, 15), 2500m),
            default);

        dto.TemPagamento.Should().BeTrue();
        dto.ValorPago.Should().Be(2500m);
        dto.DataPagamento.Should().Be(new DateOnly(2026, 10, 15));
        dto.StatusBoleto.Should().Be(nameof(StatusBoleto.Pago));
        c.Cobrancas.Pagamentos.Should().HaveCount(1);
        c.Boleto.Status.Should().Be(StatusBoleto.Pago);
        c.Cobrancas.SalvouVezes.Should().Be(1);
    }

    [Fact]
    public async Task Registrar_pagamento_ja_pago_lanca()
    {
        var c = Montar(comPagamento: true);
        var handler = new RegistrarPagamentoCobrancaCommandHandler(
            c.Cobrancas, c.Clientes, c.Planos, c.User);

        await handler.Invoking(h => h.Handle(new RegistrarPagamentoCobrancaCommand(c.Nfse.Id), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*já está marcada como paga*");
    }

    [Fact]
    public async Task Remover_cobranca_pendente_exclui_nfse_e_boleto()
    {
        var c = Montar();
        var handler = new RemoverCobrancaCommandHandler(
            c.Cobrancas, c.Clientes, c.Planos, c.User);

        var ok = await handler.Handle(new RemoverCobrancaCommand(c.Nfse.Id), default);

        ok.Should().BeTrue();
        c.Cobrancas.Nfses.Should().BeEmpty();
        c.Cobrancas.Boletos.Should().BeEmpty();
        c.Cobrancas.SalvouVezes.Should().Be(1);
    }

    [Fact]
    public async Task Remover_cobranca_paga_bloqueia()
    {
        var c = Montar(comPagamento: true);
        var handler = new RemoverCobrancaCommandHandler(
            c.Cobrancas, c.Clientes, c.Planos, c.User);

        await handler.Invoking(h => h.Handle(new RemoverCobrancaCommand(c.Nfse.Id), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*já paga*");

        c.Cobrancas.Nfses.Should().HaveCount(1);
        c.Cobrancas.SalvouVezes.Should().Be(0);
    }

    [Fact]
    public async Task Remover_inexistente_retorna_false()
    {
        var c = Montar();
        var handler = new RemoverCobrancaCommandHandler(
            c.Cobrancas, c.Clientes, c.Planos, c.User);

        var ok = await handler.Handle(new RemoverCobrancaCommand(Guid.NewGuid()), default);
        ok.Should().BeFalse();
    }
}

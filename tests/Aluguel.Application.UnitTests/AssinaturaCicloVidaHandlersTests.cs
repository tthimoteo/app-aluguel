using Aluguel.Application.Assinaturas.CancelarAssinatura;
using Aluguel.Application.Assinaturas.ContratarPlano;
using Aluguel.Application.Assinaturas.ProcessarCicloAssinaturas;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class AssinaturaCicloVidaHandlersTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid Basico = Guid.Parse("11111111-1111-1111-1111-111111111102");
    private static readonly Guid Inter = Guid.Parse("11111111-1111-1111-1111-111111111103");
    private static readonly Guid Avancado = Guid.Parse("11111111-1111-1111-1111-111111111104");

    [Fact]
    public async Task Downgrade_agenda_sem_pix()
    {
        var ctx = MontarComAssinaturaAtiva(Avancado, 249.99m);
        var handler = new ContratarPlanoCommandHandler(
            ctx.Assinaturas, ctx.Clientes, ctx.Planos, ctx.Imoveis, ctx.Usuarios,
            ctx.Pix, ctx.User, ctx.Tenant, TimeProvider.System);

        var r = await handler.Handle(new ContratarPlanoCommand(Inter), CancellationToken.None);

        r.Operacao.Should().Be("DowngradeAgendado");
        r.PagamentoId.Should().BeNull();
        r.Pagamento.Should().BeNull();
        ctx.Assinaturas.Itens[0].PlanoAgendadoId.Should().Be(Inter);
        ctx.Assinaturas.Itens[0].PlanoId.Should().Be(Avancado);
        ctx.Clientes.Itens[0].PlanoId.Should().Be(Avancado);
        ctx.Assinaturas.Auditorias.Should().Contain(a => a.Evento == EventoAssinatura.Downgrade);
    }

    [Fact]
    public async Task Upgrade_gera_pix_imediato()
    {
        var ctx = MontarComAssinaturaAtiva(Basico, 49.99m);
        var handler = new ContratarPlanoCommandHandler(
            ctx.Assinaturas, ctx.Clientes, ctx.Planos, ctx.Imoveis, ctx.Usuarios,
            ctx.Pix, ctx.User, ctx.Tenant, TimeProvider.System);

        var r = await handler.Handle(new ContratarPlanoCommand(Avancado), CancellationToken.None);

        r.Operacao.Should().Be("Upgrade");
        r.PagamentoId.Should().NotBeNull();
        r.Pagamento!.Valor.Should().Be(249.99m);
        ctx.Assinaturas.Itens[0].Status.Should().Be(StatusAssinatura.Ativa); // acesso mantido até liquidar
        ctx.Clientes.Itens[0].PlanoId.Should().Be(Basico); // só muda no webhook
    }

    [Fact]
    public async Task Cancelar_mantem_acesso_ate_fim_do_ciclo()
    {
        var ctx = MontarComAssinaturaAtiva(Inter, 149.99m);
        var handler = new CancelarAssinaturaCommandHandler(
            ctx.Assinaturas, ctx.Planos, ctx.User, ctx.Tenant, TimeProvider.System);

        var r = await handler.Handle(new CancelarAssinaturaCommand(), CancellationToken.None);

        r.FimCicloPago.Should().NotBeNull();
        ctx.Assinaturas.Itens[0].CancelamentoPendente.Should().BeTrue();
        ctx.Assinaturas.Itens[0].Status.Should().Be(StatusAssinatura.Ativa);
    }

    [Fact]
    public async Task ProcessarCiclo_efetiva_cancelamento_e_bloqueia_cliente()
    {
        var ctx = MontarComAssinaturaAtiva(Inter, 149.99m);
        var assinatura = ctx.Assinaturas.Itens[0];
        // Força fim de ciclo no passado
        typeof(Assinatura).GetProperty(nameof(Assinatura.FimCicloPago))!
            .SetValue(assinatura, DateTimeOffset.UtcNow.AddDays(-1));
        assinatura.SolicitarCancelamento(DateTimeOffset.UtcNow.AddDays(-10));

        var ciclo = new ProcessarCicloAssinaturasCommandHandler(
            ctx.Assinaturas, ctx.Clientes, ctx.Planos, ctx.Pix, TimeProvider.System);
        var r = await ciclo.Handle(new ProcessarCicloAssinaturasCommand(), CancellationToken.None);

        r.CancelamentosEfetivados.Should().Be(1);
        assinatura.Status.Should().Be(StatusAssinatura.Cancelada);
        ctx.Clientes.Itens[0].Status.Should().Be(StatusCliente.Bloqueado);
    }

    [Fact]
    public async Task ProcessarCiclo_aplica_downgrade_e_gera_renovacao()
    {
        var ctx = MontarComAssinaturaAtiva(Avancado, 249.99m);
        var assinatura = ctx.Assinaturas.Itens[0];
        assinatura.AgendarDowngrade(Inter);
        typeof(Assinatura).GetProperty(nameof(Assinatura.ProximaCobranca))!
            .SetValue(assinatura, DateTimeOffset.UtcNow.AddMinutes(-1));

        var ciclo = new ProcessarCicloAssinaturasCommandHandler(
            ctx.Assinaturas, ctx.Clientes, ctx.Planos, ctx.Pix, TimeProvider.System);
        var r = await ciclo.Handle(new ProcessarCicloAssinaturasCommand(), CancellationToken.None);

        r.DowngradesAplicados.Should().Be(1);
        r.RenovacoesGeradas.Should().Be(1);
        assinatura.PlanoId.Should().Be(Inter);
        assinatura.ValorMensal.Should().Be(149.99m);
        ctx.Clientes.Itens[0].PlanoId.Should().Be(Inter);
        assinatura.Status.Should().Be(StatusAssinatura.PendentePagamento);
        assinatura.Pagamentos.Should().Contain(p => p.Status == StatusPagamentoPlano.Pendente && p.Valor == 149.99m);
    }

    private static AssinaturaPixHandlersTestsFixture MontarComAssinaturaAtiva(Guid planoId, decimal valor)
    {
        // Reusa fakes do arquivo AssinaturaPixHandlersTests via construção local
        var planos = new FakePlanoRepository();
        planos.Itens.Add(new Plano(Basico, PlanoCodigo.Basico, "Básico", 3, 1, true, 0, 49.99m));
        planos.Itens.Add(new Plano(Inter, PlanoCodigo.Intermediario, "Intermediário", 5, 3, true, 0, 149.99m));
        planos.Itens.Add(new Plano(Avancado, PlanoCodigo.Avancado, "Avançado", 10, 5, true, 0, 249.99m, true));

        var clientes = new FakeClienteRepository();
        var cliente = Cliente.CriarPessoaFisica(
            TenantId, planoId, "Gestor Demo", "39053344705", null, null, null, Endereco.Vazio());
        clientes.Itens.Add(cliente);

        var assinaturas = new FakeAssinaturaRepository();
        var a = new Assinatura(TenantId, cliente.Id, planoId, valor, StatusAssinatura.PendentePagamento);
        a.AtivarAposPagamento(DateTimeOffset.UtcNow.AddDays(-20), planoId, valor);
        assinaturas.Adicionar(a);

        return new AssinaturaPixHandlersTestsFixture(
            assinaturas,
            clientes,
            planos,
            new FakeImovelRepository(),
            new FakeUsuarioService(),
            new FakeInterPixProvider(),
            new FakeCurrentUser(UserId, cliente.Id, false, "Gestor"),
            new FakeCurrentTenant(TenantId));
    }

    private sealed record AssinaturaPixHandlersTestsFixture(
        FakeAssinaturaRepository Assinaturas,
        FakeClienteRepository Clientes,
        FakePlanoRepository Planos,
        FakeImovelRepository Imoveis,
        FakeUsuarioService Usuarios,
        FakeInterPixProvider Pix,
        FakeCurrentUser User,
        FakeCurrentTenant Tenant);
}

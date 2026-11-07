using Aluguel.Application.Abstractions;
using Aluguel.Application.Assinaturas.ContratarPlano;
using Aluguel.Application.Assinaturas.ProcessarWebhookPixInter;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class AssinaturaPixHandlersTests
{
    private static readonly Guid TenantId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid UserId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid PlanoBasicoId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    private static readonly Guid PlanoInterId = Guid.Parse("11111111-1111-1111-1111-111111111103");
    private static readonly Guid PlanoProId = Guid.Parse("11111111-1111-1111-1111-111111111105");

    [Fact]
    public async Task ContratarPlano_gera_pagamento_pix_pendente()
    {
        var ctx = Montar();
        var handler = new ContratarPlanoCommandHandler(
            ctx.Assinaturas, ctx.Clientes, ctx.Planos, ctx.Imoveis, ctx.Usuarios,
            ctx.Pix, ctx.User, ctx.Tenant, TimeProvider.System);

        var r = await handler.Handle(new ContratarPlanoCommand(PlanoBasicoId), CancellationToken.None);

        r.Operacao.Should().Be("Contratacao");
        r.Pagamento.Should().NotBeNull();
        r.Pagamento!.Status.Should().Be("Pendente");
        r.Pagamento.PixCopiaCola.Should().NotBeNullOrWhiteSpace();
        r.Pagamento.Beneficiario.Cnpj.Should().Be("15809930000168");
        r.StatusAssinatura.Should().Be("PendentePagamento");
        ctx.Assinaturas.Itens.Should().ContainSingle();
        ctx.Assinaturas.Auditorias.Should().ContainSingle(a => a.Evento == EventoAssinatura.Contratacao);
    }

    [Fact]
    public async Task ContratarPlano_pro_sob_consulta_falha()
    {
        var ctx = Montar();
        var handler = new ContratarPlanoCommandHandler(
            ctx.Assinaturas, ctx.Clientes, ctx.Planos, ctx.Imoveis, ctx.Usuarios,
            ctx.Pix, ctx.User, ctx.Tenant, TimeProvider.System);

        var act = () => handler.Handle(new ContratarPlanoCommand(PlanoProId), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sob consulta*");
    }

    [Fact]
    public async Task Webhook_liquida_e_ativa_plano()
    {
        var ctx = Montar();
        var contratar = new ContratarPlanoCommandHandler(
            ctx.Assinaturas, ctx.Clientes, ctx.Planos, ctx.Imoveis, ctx.Usuarios,
            ctx.Pix, ctx.User, ctx.Tenant, TimeProvider.System);
        var contratado = await contratar.Handle(new ContratarPlanoCommand(PlanoInterId), CancellationToken.None);

        var pagamento = ctx.Assinaturas.Itens[0].Pagamentos.Single(p => p.Id == contratado.PagamentoId);
        var payload = $$"""
            {"pix":[{"endToEndId":"E{{Guid.NewGuid():N}}","txid":"{{pagamento.PixTxId}}","valor":"149.99"}]}
            """;

        var webhook = new ProcessarWebhookPixInterCommandHandler(
            ctx.Assinaturas, ctx.Clientes, ctx.Planos, TimeProvider.System);
        var r = await webhook.Handle(new ProcessarWebhookPixInterCommand(payload), CancellationToken.None);

        r.Processados.Should().Be(1);
        ctx.Assinaturas.Itens[0].Status.Should().Be(StatusAssinatura.Ativa);
        ctx.Clientes.Itens[0].PlanoId.Should().Be(PlanoInterId);
        pagamento.Status.Should().Be(StatusPagamentoPlano.Pago);
    }

    [Fact]
    public void ExtrairEventos_aceita_objeto_e_array()
    {
        var um = ProcessarWebhookPixInterCommandHandler.ExtrairEventos(
            """{"pix":[{"txid":"aaaaaaaaaaaaaaaaaaaaaaaaaa","endToEndId":"E1"}]}""");
        um.Should().ContainSingle().Which.TxId.Should().Be("aaaaaaaaaaaaaaaaaaaaaaaaaa");

        var varios = ProcessarWebhookPixInterCommandHandler.ExtrairEventos(
            """[{"pix":[{"txid":"bbbbbbbbbbbbbbbbbbbbbbbbbb"}]},{"pix":[{"txid":"cccccccccccccccccccccccccc"}]}]""");
        varios.Should().HaveCount(2);
    }

    private static Fixture Montar()
    {
        var planos = new FakePlanoRepository();
        planos.Itens.Add(new Plano(PlanoBasicoId, PlanoCodigo.Basico, "Básico", 3, 1, true, 0, 49.99m));
        planos.Itens.Add(new Plano(PlanoInterId, PlanoCodigo.Intermediario, "Intermediário", 5, 3, true, 0, 149.99m));
        planos.Itens.Add(new Plano(PlanoProId, PlanoCodigo.Pro, "Pro", null, null, true, 0, null, true));

        var clientes = new FakeClienteRepository();
        var cliente = Cliente.CriarPessoaFisica(
            TenantId, null, "Gestor Demo", "39053344705", null, null, null, Endereco.Vazio());
        clientes.Itens.Add(cliente);

        return new Fixture(
            new FakeAssinaturaRepository(),
            clientes,
            planos,
            new FakeImovelRepository(),
            new FakeUsuarioService(),
            new FakeInterPixProvider(),
            new FakeCurrentUser(UserId, cliente.Id, ehAdministrador: false, "Gestor"),
            new FakeCurrentTenant(TenantId));
    }

    private sealed record Fixture(
        FakeAssinaturaRepository Assinaturas,
        FakeClienteRepository Clientes,
        FakePlanoRepository Planos,
        FakeImovelRepository Imoveis,
        FakeUsuarioService Usuarios,
        FakeInterPixProvider Pix,
        FakeCurrentUser User,
        FakeCurrentTenant Tenant);
}

sealed class FakeAssinaturaRepository : IAssinaturaRepository
{
    public List<Assinatura> Itens { get; } = [];
    public List<AuditoriaAssinatura> Auditorias { get; } = [];

    public Task<Assinatura?> ObterVigentePorClienteAsync(Guid clienteId, CancellationToken ct = default) =>
        Task.FromResult(Itens.FirstOrDefault(a => a.ClienteId == clienteId &&
            a.Status is StatusAssinatura.Trial or StatusAssinatura.PendentePagamento or StatusAssinatura.Ativa));

    public Task<Assinatura?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Itens.FirstOrDefault(a => a.Id == id));

    public Task<PagamentoPlano?> ObterPagamentoPorIdAsync(Guid pagamentoId, CancellationToken ct = default) =>
        Task.FromResult(Itens.SelectMany(a => a.Pagamentos).FirstOrDefault(p => p.Id == pagamentoId));

    public Task<PagamentoPlano?> ObterPagamentoPorTxIdAsync(string pixTxId, CancellationToken ct = default) =>
        Task.FromResult(Itens.SelectMany(a => a.Pagamentos).FirstOrDefault(p => p.PixTxId == pixTxId));

    public Task<Assinatura?> ObterPorPagamentoIdAsync(Guid pagamentoId, CancellationToken ct = default) =>
        Task.FromResult(Itens.FirstOrDefault(a => a.Pagamentos.Any(p => p.Id == pagamentoId)));

    public Task<IReadOnlyList<Assinatura>> ListarParaProcessamentoCicloAsync(
        DateTimeOffset agora, bool forcarTodas, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Assinatura>>(
            forcarTodas
                ? Itens.Where(a => a.Status is StatusAssinatura.Ativa or StatusAssinatura.PendentePagamento).ToList()
                : Itens.Where(a =>
                    (a.CancelamentoSolicitadoEm != null && a.Status == StatusAssinatura.Ativa
                        && (a.FimCicloPago == null || a.FimCicloPago <= agora))
                    || (a.Status == StatusAssinatura.Ativa && a.CancelamentoSolicitadoEm == null
                        && a.ProximaCobranca != null && a.ProximaCobranca <= agora)
                    || a.Status == StatusAssinatura.PendentePagamento).ToList());

    public void Adicionar(Assinatura assinatura) => Itens.Add(assinatura);

    public void AdicionarPagamento(PagamentoPlano pagamento)
    {
        // Já entra na coleção da assinatura via CriarPagamentoPix; no-op em memória.
        _ = pagamento;
    }

    public void AdicionarAuditoria(AuditoriaAssinatura auditoria) => Auditorias.Add(auditoria);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) => Task.FromResult(1);
}

sealed class FakePlanoRepository : IPlanoRepository
{
    public List<Plano> Itens { get; } = [];

    public Task<Plano?> ObterPorIdAsync(Guid id, CancellationToken ct = default) =>
        Task.FromResult(Itens.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Plano>> ListarAtivosAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<Plano>>(Itens.Where(p => p.Ativo).ToList());
}

sealed class FakeInterPixProvider : IInterPixProvider
{
    public InterPixBeneficiario ObterBeneficiario() =>
        new("TTHIMOTEO Consultoria e Serviços de Eng. Ltda", "15809930000168", "Banco Inter", "44588184-4");

    public Task<InterPixCobrancaResultado> CriarCobrancaImediataAsync(
        InterPixCobrancaPedido pedido, CancellationToken ct = default) =>
        Task.FromResult(new InterPixCobrancaResultado(
            true, pedido.TxId, $"PIXMOCK{pedido.TxId}", null, $"mock://{pedido.TxId}", "ATIVA"));
}

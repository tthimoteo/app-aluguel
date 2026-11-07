using Aluguel.Application.Abstractions;
using Aluguel.Application.CadastroPublico.RegistrarContaPublica;
using Aluguel.Application.CadastroPublico.SolicitarDemonstracao;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Tenancy;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class CadastroPublicoHandlersTests
{
    private static readonly Guid PlanoTrialId = Guid.Parse("11111111-1111-1111-1111-111111111101");
    private static readonly Guid PlanoBasicoId = Guid.Parse("11111111-1111-1111-1111-111111111102");
    private static readonly Guid PlanoProId = Guid.Parse("11111111-1111-1111-1111-111111111105");

    [Fact]
    public async Task Registrar_trial_cria_assinatura_e_usuario()
    {
        var ctx = Montar();
        var handler = new RegistrarContaPublicaCommandHandler(
            ctx.Tenants, ctx.Clientes, ctx.Assinaturas, ctx.Planos, ctx.Usuarios, ctx.Pix, ctx.Auth,
            TimeProvider.System);

        var r = await handler.Handle(new RegistrarContaPublicaCommand(
            "Maria Locadora", "maria@exemplo.com", "11987654321", "39053344705",
            "Senha@123456", null), CancellationToken.None);

        r.Operacao.Should().Be("Trial");
        r.StatusAssinatura.Should().Be("Trial");
        r.PagamentoId.Should().BeNull();
        r.Tokens.AccessToken.Should().NotBeNullOrWhiteSpace();
        ctx.Tenants.Itens.Should().ContainSingle();
        ctx.Clientes.Itens.Should().ContainSingle();
        ctx.Assinaturas.Itens.Should().ContainSingle(a => a.Status == StatusAssinatura.Trial);
        ctx.Usuarios.CriouVezes.Should().Be(1);
        ctx.Auth.LoginChamado.Should().BeTrue();
    }

    [Fact]
    public async Task Registrar_plano_pago_gera_pix()
    {
        var ctx = Montar();
        var handler = new RegistrarContaPublicaCommandHandler(
            ctx.Tenants, ctx.Clientes, ctx.Assinaturas, ctx.Planos, ctx.Usuarios, ctx.Pix, ctx.Auth,
            TimeProvider.System);

        var r = await handler.Handle(new RegistrarContaPublicaCommand(
            "João Proprietário", "joao@exemplo.com", "11912345678", "52998224725",
            "Senha@123456", PlanoBasicoId), CancellationToken.None);

        r.Operacao.Should().Be("Contratacao");
        r.Pagamento.Should().NotBeNull();
        r.Pagamento!.PixCopiaCola.Should().NotBeNullOrWhiteSpace();
        r.StatusAssinatura.Should().Be("PendentePagamento");
        ctx.Assinaturas.Itens.Should().ContainSingle(a => a.Status == StatusAssinatura.PendentePagamento);
    }

    [Fact]
    public async Task Registrar_pro_rejeita()
    {
        var ctx = Montar();
        var handler = new RegistrarContaPublicaCommandHandler(
            ctx.Tenants, ctx.Clientes, ctx.Assinaturas, ctx.Planos, ctx.Usuarios, ctx.Pix, ctx.Auth,
            TimeProvider.System);

        var act = () => handler.Handle(new RegistrarContaPublicaCommand(
            "Holding Imóveis", "holding@exemplo.com", "11911112222", "11144477735",
            "Senha@123456", PlanoProId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*demonstração*");
    }

    [Fact]
    public async Task Demonstracao_valida_campos()
    {
        var handler = new SolicitarDemonstracaoCommandHandler();
        var r = await handler.Handle(new SolicitarDemonstracaoCommand(
            "Ana Admin", "ana@empresa.com", "11999990000", "Quero o plano Pro"), CancellationToken.None);
        r.Ok.Should().BeTrue();

        var act = () => handler.Handle(new SolicitarDemonstracaoCommand("A", "x", "1", null), CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    private static Fixture Montar()
    {
        var planos = new FakePlanoRepository();
        planos.Itens.Add(new Plano(PlanoTrialId, PlanoCodigo.Trial, "Trial", 1, 1, false, 7, 0m));
        planos.Itens.Add(new Plano(PlanoBasicoId, PlanoCodigo.Basico, "Básico", 3, 1, true, 0, 49.99m));
        planos.Itens.Add(new Plano(PlanoProId, PlanoCodigo.Pro, "Pro", null, null, true, 0, null, true));

        var auth = new FakeAuthServiceComLogin();
        return new Fixture(
            new FakeTenantStore(),
            new FakeClienteRepository(),
            new FakeAssinaturaRepository(),
            planos,
            new FakeUsuarioService(),
            new FakeInterPixProvider(),
            auth);
    }

    private sealed record Fixture(
        FakeTenantStore Tenants,
        FakeClienteRepository Clientes,
        FakeAssinaturaRepository Assinaturas,
        FakePlanoRepository Planos,
        FakeUsuarioService Usuarios,
        FakeInterPixProvider Pix,
        FakeAuthServiceComLogin Auth);
}

sealed class FakeTenantStore : ITenantStore
{
    public List<Tenant> Itens { get; } = [];

    public Task<bool> SubdominioEmUsoAsync(string subdominio, CancellationToken ct = default) =>
        Task.FromResult(Itens.Any(t => t.Subdominio == subdominio));

    public void Adicionar(Tenant tenant) => Itens.Add(tenant);

    public Task<int> SalvarAlteracoesAsync(CancellationToken ct = default) => Task.FromResult(1);
}

sealed class FakeAuthServiceComLogin : IAuthService
{
    public bool LoginChamado { get; private set; }

    public Task<ResultadoAuth> LoginAsync(string email, string senha, string? ip, CancellationToken ct = default)
    {
        LoginChamado = true;
        var tokens = new TokensAutenticacao(
            "access-token",
            DateTimeOffset.UtcNow.AddHours(1),
            "refresh-token",
            DateTimeOffset.UtcNow.AddDays(7),
            new UsuarioAutenticado(
                Guid.NewGuid(), email, "Teste", Guid.NewGuid(), Guid.NewGuid(),
                ["Gestor"], []));
        return Task.FromResult(ResultadoAuth.Ok(tokens));
    }

    public Task<ResultadoAuth> RefreshAsync(string refreshToken, string? ip, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task LogoutAsync(string refreshToken, CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<ResultadoAuth> SelecionarClienteAsync(Guid userId, Guid clienteId, string refreshToken, string? ip,
        CancellationToken ct = default) =>
        throw new NotImplementedException();

    public Task<IReadOnlyList<VinculoClienteDto>> ListarVinculosAsync(Guid userId, CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<VinculoClienteDto>>([]);
}

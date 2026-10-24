using Aluguel.Application.Clientes;
using Aluguel.Application.Clientes.AtualizarCliente;
using Aluguel.Application.Clientes.CriarCliente;
using Aluguel.Application.Clientes.RemoverCliente;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Assinaturas;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Imoveis;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class ClienteHandlersTests
{
    private static readonly Guid Tenant = Guid.Parse("2e8c8210-56a6-4078-9cde-f61fd6142ae8");

    private static AtualizarClienteCommandHandler HandlerAtualizar(
        FakeClienteRepository repo,
        FakeCurrentUser user,
        FakePlanoRepository? planos = null,
        FakeImovelRepository? imoveis = null,
        FakeUsuarioService? usuarios = null) =>
        new(repo, planos ?? new FakePlanoRepository(), imoveis ?? new FakeImovelRepository(),
            usuarios ?? new FakeUsuarioService(), user);

    [Fact]
    public async Task Criar_define_tenant_do_contexto_e_persiste()
    {
        var repo = new FakeClienteRepository();
        var handler = new CriarClienteCommandHandler(repo, new FakeCurrentTenant(Tenant));

        var cmd = new CriarClienteCommand(TipoPessoa.PF, null, "João", "390.533.447-05", null,
            null, null, null, null, null, null, null,
            new EnderecoInput("Rua A", "10", null, "Centro", "SP", "sp", "01001000"));

        var dto = await handler.Handle(cmd, default);

        dto.TenantId.Should().Be(Tenant);
        dto.Cpf.Should().Be("39053344705");            // máscara removida
        dto.Endereco.Uf.Should().Be("SP");             // normalizado p/ maiúsculas
        dto.TipoPessoa.Should().Be("PF");
        repo.Itens.Should().ContainSingle();
        repo.SalvouVezes.Should().Be(1);
    }

    [Fact]
    public async Task Criar_sem_tenant_lanca()
    {
        var handler = new CriarClienteCommandHandler(new FakeClienteRepository(), new FakeCurrentTenant(null));
        var cmd = new CriarClienteCommand(TipoPessoa.PF, null, "João", "39053344705", null,
            null, null, null, null, null, null, null, null);

        var act = () => handler.Handle(cmd, default);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Atualizar_inexistente_retorna_null()
    {
        var admin = new FakeCurrentUser(Guid.NewGuid(), clienteId: null, ehAdministrador: true);
        var handler = HandlerAtualizar(new FakeClienteRepository(), admin);
        var cmd = new AtualizarClienteCommand(Guid.NewGuid(), null, "Novo", null,
            null, null, null, null, null, null, null, null, null, null, null);

        (await handler.Handle(cmd, default)).Should().BeNull();
    }

    [Fact]
    public async Task Atualizar_pf_altera_dados()
    {
        var repo = new FakeClienteRepository();
        var cliente = Cliente.CriarPessoaFisica(Tenant, null, "Antigo", "39053344705", null, null, null, null);
        repo.Adicionar(cliente);
        var admin = new FakeCurrentUser(Guid.NewGuid(), clienteId: null, ehAdministrador: true);
        var handler = HandlerAtualizar(repo, admin);

        var dto = await handler.Handle(new AtualizarClienteCommand(cliente.Id, null, "Novo Nome", null,
            null, null, null, null, null, null, null, null, "11888887777", "novo@x.com", null), default);

        dto!.Nome.Should().Be("Novo Nome");
        dto.Telefone.Should().Be("11888887777");
        repo.SalvouVezes.Should().Be(1);
    }

    [Fact]
    public async Task Atualizar_fora_do_escopo_retorna_null()
    {
        var repo = new FakeClienteRepository();
        var cliente = Cliente.CriarPessoaFisica(Tenant, null, "Antigo", "39053344705", null, null, null, null);
        repo.Adicionar(cliente);
        var outroCliente = Guid.NewGuid();
        var gestor = new FakeCurrentUser(Guid.NewGuid(), outroCliente, ehAdministrador: false);
        var handler = HandlerAtualizar(repo, gestor);

        var dto = await handler.Handle(new AtualizarClienteCommand(cliente.Id, null, "Novo Nome", null,
            null, null, null, null, null, null, null, null, null, null, null), default);

        dto.Should().BeNull();
        repo.SalvouVezes.Should().Be(0);
    }

    [Fact]
    public async Task Atualizar_admin_altera_status_operacional()
    {
        var repo = new FakeClienteRepository();
        var cliente = Cliente.CriarPessoaFisica(Tenant, null, "Antigo", "39053344705", null, null, null, null);
        repo.Adicionar(cliente);
        var admin = new FakeCurrentUser(Guid.NewGuid(), clienteId: null, ehAdministrador: true);
        var handler = HandlerAtualizar(repo, admin);

        var dto = await handler.Handle(new AtualizarClienteCommand(cliente.Id, null, "Antigo", null,
            null, null, null, null, null, null, null, null, null, null, null, StatusCliente.Bloqueado), default);

        dto!.Status.Should().Be(nameof(StatusCliente.Bloqueado));
        cliente.Status.Should().Be(StatusCliente.Bloqueado);
    }

    [Fact]
    public async Task Atualizar_plano_com_imoveis_excedentes_bloqueia()
    {
        var repo = new FakeClienteRepository();
        var planoAvancado = new Plano(Guid.NewGuid(), PlanoCodigo.Avancado, "Avançado", 10, 5, true, 0, 249.99m);
        var planoTrial = new Plano(Guid.NewGuid(), PlanoCodigo.Trial, "Trial", 1, 1, false, 7, 0m);
        var cliente = Cliente.CriarPessoaFisica(Tenant, planoAvancado.Id, "Locador", "39053344705", null, null, null, null);
        repo.Adicionar(cliente);

        var planos = new FakePlanoRepository();
        planos.Itens.Add(planoAvancado);
        planos.Itens.Add(planoTrial);

        var imoveis = new FakeImovelRepository();
        imoveis.Itens.Add(new Imovel(Tenant, cliente.Id, "Apto 1", TipoImovel.Residencial));
        imoveis.Itens.Add(new Imovel(Tenant, cliente.Id, "Apto 2", TipoImovel.Residencial));
        imoveis.Itens.Add(new Imovel(Tenant, cliente.Id, "Apto 3", TipoImovel.Residencial));

        var admin = new FakeCurrentUser(Guid.NewGuid(), clienteId: null, ehAdministrador: true);
        var handler = HandlerAtualizar(repo, admin, planos, imoveis);

        var act = () => handler.Handle(new AtualizarClienteCommand(cliente.Id, planoTrial.Id, "Locador", null,
            null, null, null, null, null, null, null, null, null, null, null), default);

        var ex = await act.Should().ThrowAsync<InvalidOperationException>();
        ex.Which.Message.Should().Contain("Trial");
        ex.Which.Message.Should().Contain("3 imóvel");
        ex.Which.Message.Should().Contain("no máximo 1");
        cliente.PlanoId.Should().Be(planoAvancado.Id);
        repo.SalvouVezes.Should().Be(0);
    }

    [Fact]
    public async Task Atualizar_plano_compativel_aplica()
    {
        var repo = new FakeClienteRepository();
        var planoTrial = new Plano(Guid.NewGuid(), PlanoCodigo.Trial, "Trial", 1, 1, false, 7, 0m);
        var planoBasico = new Plano(Guid.NewGuid(), PlanoCodigo.Basico, "Básico", 3, 1, false, 0, 49.99m);
        var cliente = Cliente.CriarPessoaFisica(Tenant, planoTrial.Id, "Locador", "39053344705", null, null, null, null);
        repo.Adicionar(cliente);

        var planos = new FakePlanoRepository();
        planos.Itens.Add(planoTrial);
        planos.Itens.Add(planoBasico);

        var imoveis = new FakeImovelRepository();
        imoveis.Itens.Add(new Imovel(Tenant, cliente.Id, "Apto 1", TipoImovel.Residencial));

        var admin = new FakeCurrentUser(Guid.NewGuid(), clienteId: null, ehAdministrador: true);
        var handler = HandlerAtualizar(repo, admin, planos, imoveis);

        var dto = await handler.Handle(new AtualizarClienteCommand(cliente.Id, planoBasico.Id, "Locador", null,
            null, null, null, null, null, null, null, null, null, null, null), default);

        dto!.PlanoId.Should().Be(planoBasico.Id);
        cliente.PlanoId.Should().Be(planoBasico.Id);
    }

    [Fact]
    public async Task Remover_inexistente_retorna_false()
    {
        var handler = new RemoverClienteCommandHandler(new FakeClienteRepository());
        (await handler.Handle(new RemoverClienteCommand(Guid.NewGuid()), default)).Should().BeFalse();
    }

    [Fact]
    public async Task Remover_existente_faz_soft_delete()
    {
        var repo = new FakeClienteRepository();
        var cliente = Cliente.CriarPessoaFisica(Tenant, null, "João", "39053344705", null, null, null, null);
        repo.Adicionar(cliente);
        var handler = new RemoverClienteCommandHandler(repo);

        var ok = await handler.Handle(new RemoverClienteCommand(cliente.Id), default);

        ok.Should().BeTrue();
        cliente.DeletedAt.Should().NotBeNull();
    }
}

using Aluguel.Application.Clientes;
using Aluguel.Application.Clientes.AtualizarCliente;
using Aluguel.Application.Clientes.CriarCliente;
using Aluguel.Application.Clientes.RemoverCliente;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Clientes;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class ClienteHandlersTests
{
    private static readonly Guid Tenant = Guid.Parse("2e8c8210-56a6-4078-9cde-f61fd6142ae8");

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
        var handler = new AtualizarClienteCommandHandler(new FakeClienteRepository());
        var cmd = new AtualizarClienteCommand(Guid.NewGuid(), null, "Novo", null,
            null, null, null, null, null, null, null);

        (await handler.Handle(cmd, default)).Should().BeNull();
    }

    [Fact]
    public async Task Atualizar_pf_altera_dados()
    {
        var repo = new FakeClienteRepository();
        var cliente = Cliente.CriarPessoaFisica(Tenant, null, "Antigo", "39053344705", null, null, null, null);
        repo.Adicionar(cliente);
        var handler = new AtualizarClienteCommandHandler(repo);

        var dto = await handler.Handle(new AtualizarClienteCommand(cliente.Id, null, "Novo Nome", null,
            null, null, null, null, "11888887777", "novo@x.com", null), default);

        dto!.Nome.Should().Be("Novo Nome");
        dto.Telefone.Should().Be("11888887777");
        repo.SalvouVezes.Should().Be(1);
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

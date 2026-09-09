using Aluguel.Application.Abstractions;
using Aluguel.Application.Imoveis.AtualizarImovel;
using Aluguel.Application.Imoveis.CriarImovel;
using Aluguel.Application.Imoveis.ListarImoveis;
using Aluguel.Application.Imoveis.RemoverImovel;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Common;
using Aluguel.Domain.Imoveis;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class ImovelTests
{
    private static readonly Guid Tenant = Guid.Parse("2e8c8210-56a6-4078-9cde-f61fd6142ae8");
    private static readonly Guid ClienteA = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");
    private static readonly Guid ClienteB = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static CriarImovelCommand Comando(string nome = "Apto 101", TipoImovel tipo = TipoImovel.Residencial) =>
        new(ClienteA, nome, tipo, null, null, null);

    [Fact]
    public async Task Validator_imovel_valido_passa()
    {
        var result = await new CriarImovelCommandValidator(new FakeImovelRepository()).TestValidateAsync(Comando());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validator_nome_vazio_falha()
    {
        var result = await new CriarImovelCommandValidator(new FakeImovelRepository()).TestValidateAsync(Comando(nome: " "));
        result.ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public async Task Validator_limite_do_plano_falha()
    {
        var repo = new FakeImovelRepository { PodeAdicionar = false };
        var result = await new CriarImovelCommandValidator(repo).TestValidateAsync(Comando());
        result.ShouldHaveValidationErrorFor(x => x.ClienteId);
    }

    [Fact]
    public async Task Criar_define_tenant_e_cliente()
    {
        var repo = new FakeImovelRepository();
        var handler = new CriarImovelCommandHandler(repo, new FakeCurrentTenant(Tenant));

        var dto = await handler.Handle(Comando(nome: "  Casa  "), default);

        dto.TenantId.Should().Be(Tenant);
        dto.ClienteId.Should().Be(ClienteA);
        dto.Nome.Should().Be("Casa");
        repo.SalvouVezes.Should().Be(1);
    }

    [Fact]
    public async Task Criar_com_limite_atingido_lanca()
    {
        var repo = new FakeImovelRepository { PodeAdicionar = false };
        var handler = new CriarImovelCommandHandler(repo, new FakeCurrentTenant(Tenant));
        await handler.Invoking(h => h.Handle(Comando(), default))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Remover_com_contrato_ativo_lanca()
    {
        var repo = new FakeImovelRepository();
        var imovel = new Imovel(Tenant, ClienteA, "Apto", TipoImovel.Residencial);
        repo.Itens.Add(imovel);
        repo.ImoveisComContratoAtivo.Add(imovel.Id);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new RemoverImovelCommandHandler(repo, gestor);

        await handler.Invoking(h => h.Handle(new RemoverImovelCommand(imovel.Id), default))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Remover_fora_do_escopo_retorna_false()
    {
        var repo = new FakeImovelRepository();
        var imovel = new Imovel(Tenant, ClienteB, "Apto", TipoImovel.Residencial);
        repo.Itens.Add(imovel);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new RemoverImovelCommandHandler(repo, gestor);

        (await handler.Handle(new RemoverImovelCommand(imovel.Id), default)).Should().BeFalse();
    }

    [Fact]
    public async Task Remover_no_escopo_sem_dependentes_ok()
    {
        var repo = new FakeImovelRepository();
        var imovel = new Imovel(Tenant, ClienteA, "Apto", TipoImovel.Residencial);
        repo.Itens.Add(imovel);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new RemoverImovelCommandHandler(repo, gestor);

        (await handler.Handle(new RemoverImovelCommand(imovel.Id), default)).Should().BeTrue();
    }

    [Fact]
    public async Task Listar_como_gestor_de_cliente_vinculado_filtra_o_solicitado()
    {
        var repo = new FakeImovelRepository();
        repo.Itens.Add(new Imovel(Tenant, ClienteA, "Apto A", TipoImovel.Residencial));
        repo.Itens.Add(new Imovel(Tenant, ClienteB, "Apto B", TipoImovel.Residencial));

        var auth = new FakeAuthService();
        auth.Vinculos.Add(new VinculoClienteDto(ClienteB, "Cliente B", "Analista", "Ativo"));

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new ListarImoveisQueryHandler(repo, gestor, auth);

        var pagina = await handler.Handle(new ListarImoveisQuery(ClienteId: ClienteB), default);

        pagina.Total.Should().Be(1);
        pagina.Itens.Should().OnlyContain(i => i.ClienteId == ClienteB);
    }

    [Fact]
    public async Task Reativar_com_limite_atingido_lanca()
    {
        var repo = new FakeImovelRepository { PodeAdicionar = false };
        var imovel = new Imovel(Tenant, ClienteA, "Apto", TipoImovel.Residencial);
        imovel.Inativar();
        repo.Itens.Add(imovel);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new AtualizarImovelCommandHandler(repo, gestor);

        await handler.Invoking(h => h.Handle(
                new AtualizarImovelCommand(imovel.Id, "Apto", TipoImovel.Residencial, null, null, StatusAtivoInativo.Ativo, null), default))
            .Should().ThrowAsync<InvalidOperationException>();
    }
}

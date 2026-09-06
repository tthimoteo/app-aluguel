using Aluguel.Application.Contratos.CancelarContrato;
using Aluguel.Application.Contratos.CriarContrato;
using Aluguel.Application.Contratos.EncerrarContrato;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Contratos;
using Aluguel.Domain.Imoveis;
using Aluguel.Domain.Inquilinos;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class ContratoHandlersTests
{
    private static readonly Guid Tenant = Guid.Parse("2e8c8210-56a6-4078-9cde-f61fd6142ae8");
    private static readonly Guid ClienteA = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");
    private static readonly Guid ClienteB = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private sealed record Cenario(
        FakeContratoRepository Contratos,
        FakeImovelRepository Imoveis,
        FakeInquilinoRepository Inquilinos,
        Imovel Imovel,
        Inquilino Inquilino,
        CriarContratoCommandHandler Handler);

    private static Cenario Montar(Guid clienteImovel, Guid clienteInquilino, bool admin = false)
    {
        var contratos = new FakeContratoRepository();
        var imoveis = new FakeImovelRepository();
        var inquilinos = new FakeInquilinoRepository();

        var imovel = new Imovel(Tenant, clienteImovel, "Apto 101", TipoImovel.Residencial);
        var inquilino = new Inquilino(Tenant, clienteInquilino, TipoPessoa.PF, "João", "39053344705");
        imoveis.Itens.Add(imovel);
        inquilinos.Itens.Add(inquilino);

        var atual = admin
            ? new FakeCurrentUser(Guid.NewGuid(), null, ehAdministrador: true)
            : new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);

        var handler = new CriarContratoCommandHandler(contratos, imoveis, inquilinos, new FakeCurrentTenant(Tenant), atual);
        return new Cenario(contratos, imoveis, inquilinos, imovel, inquilino, handler);
    }

    private static CriarContratoCommand Cmd(Guid imovelId, Guid inquilinoId) =>
        new(imovelId, inquilinoId, "CT-2026-001", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31),
            10, 2500m, null, null);

    [Fact]
    public async Task Criar_deriva_cliente_do_imovel_e_salva()
    {
        var c = Montar(ClienteA, ClienteA);
        var dto = await c.Handler.Handle(Cmd(c.Imovel.Id, c.Inquilino.Id), default);

        dto.ClienteId.Should().Be(ClienteA);
        dto.TenantId.Should().Be(Tenant);
        dto.Status.Should().Be("Ativo");
        c.Contratos.SalvouVezes.Should().Be(1);
    }

    [Fact]
    public async Task Criar_com_imovel_ja_com_contrato_ativo_lanca_CASO3()
    {
        var c = Montar(ClienteA, ClienteA);
        c.Contratos.Itens.Add(new Contrato(Tenant, ClienteA, c.Imovel.Id, Guid.NewGuid(), "CT-EXISTENTE",
            new DateOnly(2025, 1, 1), 10, 1000m));

        await c.Handler.Invoking(h => h.Handle(Cmd(c.Imovel.Id, c.Inquilino.Id), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*contrato ativo*");
    }

    [Fact]
    public async Task Criar_com_imovel_e_inquilino_de_clientes_diferentes_lanca()
    {
        var c = Montar(ClienteA, ClienteB, admin: true);
        await c.Handler.Invoking(h => h.Handle(Cmd(c.Imovel.Id, c.Inquilino.Id), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*mesmo cliente*");
    }

    [Fact]
    public async Task Criar_com_imovel_inexistente_lanca()
    {
        var c = Montar(ClienteA, ClienteA);
        await c.Handler.Invoking(h => h.Handle(Cmd(Guid.NewGuid(), c.Inquilino.Id), default))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Imóvel*");
    }

    [Fact]
    public async Task Gestor_nao_cria_contrato_em_imovel_de_outro_cliente()
    {
        var c = Montar(ClienteB, ClienteB); // imóvel/inquilino do ClienteB, gestor do ClienteA
        await c.Handler.Invoking(h => h.Handle(Cmd(c.Imovel.Id, c.Inquilino.Id), default))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Encerrar_libera_imovel_para_novo_contrato()
    {
        var contratos = new FakeContratoRepository();
        var contrato = new Contrato(Tenant, ClienteA, Guid.NewGuid(), Guid.NewGuid(), "CT-1",
            new DateOnly(2026, 1, 1), 10, 2000m);
        contratos.Itens.Add(contrato);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new EncerrarContratoCommandHandler(contratos, gestor);

        var dto = await handler.Handle(new EncerrarContratoCommand(contrato.Id), default);

        dto!.Status.Should().Be("Encerrado");
        (await contratos.ImovelPossuiContratoAtivoAsync(contrato.ImovelId, null, default)).Should().BeFalse();
    }

    [Fact]
    public async Task Cancelar_fora_do_escopo_retorna_null()
    {
        var contratos = new FakeContratoRepository();
        var contrato = new Contrato(Tenant, ClienteB, Guid.NewGuid(), Guid.NewGuid(), "CT-1",
            new DateOnly(2026, 1, 1), 10, 2000m);
        contratos.Itens.Add(contrato);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new CancelarContratoCommandHandler(contratos, gestor);

        (await handler.Handle(new CancelarContratoCommand(contrato.Id), default)).Should().BeNull();
    }
}

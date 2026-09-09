using Aluguel.Application.Abstractions;
using Aluguel.Application.Usuarios;
using Aluguel.Application.Usuarios.AtualizarUsuario;
using Aluguel.Application.Usuarios.CriarUsuario;
using Aluguel.Application.Usuarios.ListarUsuarios;
using Aluguel.Application.Usuarios.ObterUsuarioPorId;
using Aluguel.Application.Usuarios.RemoverUsuario;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Usuarios;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class UsuarioHandlersTests
{
    private static readonly Guid Tenant = Guid.Parse("2e8c8210-56a6-4078-9cde-f61fd6142ae8");
    private static readonly Guid ClienteA = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");
    private static readonly Guid ClienteB = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static UsuarioDto Usuario(Guid id, Guid cliente, string status = "Ativo") =>
        new(id, Tenant, cliente, "Nome", "u@demo.local", null, null, "Analista", status, null);

    private static FakeAuthService Auth(params (Guid clienteId, string perfil)[] vinculos)
    {
        var auth = new FakeAuthService();
        foreach (var (clienteId, perfil) in vinculos)
            auth.Vinculos.Add(new VinculoClienteDto(clienteId, "Cliente", perfil, "Ativo"));
        return auth;
    }

    [Fact]
    public async Task Criar_define_tenant_do_contexto_e_normaliza_cpf()
    {
        var service = new FakeUsuarioService();
        var handler = new CriarUsuarioCommandHandler(service, new FakeCurrentTenant(Tenant));

        var cmd = new CriarUsuarioCommand(ClienteA, "  Ana  ", "  ana@demo.local ",
            "390.533.447-05", "11999998888", PerfilUsuario.Analista, "Senha@12345");

        var dto = await handler.Handle(cmd, default);

        dto.TenantId.Should().Be(Tenant);
        service.CriouVezes.Should().Be(1);
        service.UltimoCriado!.Cpf.Should().Be("39053344705");   // máscara removida
        service.UltimoCriado!.Email.Should().Be("ana@demo.local"); // trim
        service.UltimoCriado!.ClienteId.Should().Be(ClienteA);
    }

    [Fact]
    public async Task Criar_sem_tenant_lanca()
    {
        var handler = new CriarUsuarioCommandHandler(new FakeUsuarioService(), new FakeCurrentTenant(null));
        var cmd = new CriarUsuarioCommand(ClienteA, "Ana", "ana@demo.local", null, null,
            PerfilUsuario.Analista, "Senha@12345");

        var act = () => handler.Handle(cmd, default);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Gestor_nao_atualiza_usuario_sem_vinculo()
    {
        var service = new FakeUsuarioService();
        var alvo = Usuario(Guid.NewGuid(), ClienteB);
        service.Itens.Add(alvo);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new AtualizarUsuarioCommandHandler(service, gestor, Auth());

        var act = () => handler.Handle(new AtualizarUsuarioCommand(alvo.Id, ClienteB, "Novo", null,
            PerfilUsuario.Analista, StatusUsuario.Ativo), default);

        await act.Should().ThrowAsync<InvalidOperationException>();
        service.UltimaAtualizacao.Should().BeNull();
    }

    [Fact]
    public async Task Gestor_atualiza_usuario_do_proprio_cliente()
    {
        var service = new FakeUsuarioService();
        var alvo = Usuario(Guid.NewGuid(), ClienteA);
        service.Itens.Add(alvo);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new AtualizarUsuarioCommandHandler(service, gestor, Auth());

        var dto = await handler.Handle(new AtualizarUsuarioCommand(alvo.Id, ClienteA, "Novo Nome", "11333334444",
            PerfilUsuario.Gestor, StatusUsuario.Ativo), default);

        dto!.Nome.Should().Be("Novo Nome");
        dto.Perfil.Should().Be("Gestor");
    }

    [Fact]
    public async Task Gestor_atualiza_usuario_de_cliente_vinculado()
    {
        var service = new FakeUsuarioService();
        var alvo = Usuario(Guid.NewGuid(), ClienteB);
        service.Itens.Add(alvo);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new AtualizarUsuarioCommandHandler(service, gestor, Auth((ClienteB, "Gestor")));

        var dto = await handler.Handle(new AtualizarUsuarioCommand(alvo.Id, ClienteB, "Outro", null,
            PerfilUsuario.Analista, StatusUsuario.Ativo), default);

        dto!.Nome.Should().Be("Outro");
        service.UltimoClienteAtualizado.Should().Be(ClienteB);
    }

    [Fact]
    public async Task Administrador_atualiza_qualquer_cliente()
    {
        var service = new FakeUsuarioService();
        var alvo = Usuario(Guid.NewGuid(), ClienteB);
        service.Itens.Add(alvo);

        var admin = new FakeCurrentUser(Guid.NewGuid(), clienteId: null, ehAdministrador: true);
        var handler = new AtualizarUsuarioCommandHandler(service, admin, Auth());

        var dto = await handler.Handle(new AtualizarUsuarioCommand(alvo.Id, ClienteB, "Editado", null,
            PerfilUsuario.Analista, StatusUsuario.Inativo), default);

        dto!.Nome.Should().Be("Editado");
        dto.Status.Should().Be("Inativo");
    }

    [Fact]
    public async Task Remover_o_proprio_usuario_lanca()
    {
        var service = new FakeUsuarioService();
        var eu = Usuario(Guid.NewGuid(), ClienteA);
        service.Itens.Add(eu);

        var gestor = new FakeCurrentUser(eu.Id, ClienteA, ehAdministrador: false);
        var handler = new RemoverUsuarioCommandHandler(service, gestor, Auth());

        var act = () => handler.Handle(new RemoverUsuarioCommand(eu.Id), default);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Remover_usuario_de_outro_cliente_retorna_false()
    {
        var service = new FakeUsuarioService();
        var alvo = Usuario(Guid.NewGuid(), ClienteB);
        service.Itens.Add(alvo);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new RemoverUsuarioCommandHandler(service, gestor, Auth());

        (await handler.Handle(new RemoverUsuarioCommand(alvo.Id), default)).Should().BeFalse();
        service.RemoveuVezes.Should().Be(0);
    }

    [Fact]
    public async Task Remover_usuario_no_escopo_desativa()
    {
        var service = new FakeUsuarioService();
        var alvo = Usuario(Guid.NewGuid(), ClienteA);
        service.Itens.Add(alvo);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new RemoverUsuarioCommandHandler(service, gestor, Auth());

        (await handler.Handle(new RemoverUsuarioCommand(alvo.Id), default)).Should().BeTrue();
        service.RemoveuVezes.Should().Be(1);
    }

    [Fact]
    public async Task Obter_fora_do_escopo_retorna_null()
    {
        var service = new FakeUsuarioService();
        var alvo = Usuario(Guid.NewGuid(), ClienteB);
        service.Itens.Add(alvo);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new ObterUsuarioPorIdQueryHandler(service, gestor, Auth());

        (await handler.Handle(new ObterUsuarioPorIdQuery(alvo.Id), default)).Should().BeNull();
    }

    [Fact]
    public async Task Listar_como_gestor_sem_clienteId_usa_o_proprio_cliente()
    {
        var service = new FakeUsuarioService();
        service.Itens.Add(Usuario(Guid.NewGuid(), ClienteA));
        service.Itens.Add(Usuario(Guid.NewGuid(), ClienteB));

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new ListarUsuariosQueryHandler(service, gestor, Auth());

        var pagina = await handler.Handle(new ListarUsuariosQuery(), default);

        pagina.Total.Should().Be(1);
        pagina.Itens.Should().OnlyContain(u => u.ClienteId == ClienteA);
    }

    [Fact]
    public async Task Listar_como_gestor_de_cliente_vinculado_usa_o_solicitado()
    {
        var service = new FakeUsuarioService();
        service.Itens.Add(Usuario(Guid.NewGuid(), ClienteA));
        service.Itens.Add(Usuario(Guid.NewGuid(), ClienteB));

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new ListarUsuariosQueryHandler(service, gestor, Auth((ClienteB, "Gestor")));

        var pagina = await handler.Handle(new ListarUsuariosQuery(ClienteId: ClienteB), default);

        pagina.Total.Should().Be(1);
        pagina.Itens.Should().OnlyContain(u => u.ClienteId == ClienteB);
    }

    [Fact]
    public async Task Listar_como_gestor_sem_vinculo_lanca()
    {
        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new ListarUsuariosQueryHandler(new FakeUsuarioService(), gestor, Auth());

        var act = () => handler.Handle(new ListarUsuariosQuery(ClienteId: ClienteB), default);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Listar_como_admin_sem_cliente_lanca()
    {
        var admin = new FakeCurrentUser(Guid.NewGuid(), clienteId: null, ehAdministrador: true);
        var handler = new ListarUsuariosQueryHandler(new FakeUsuarioService(), admin, Auth());

        var act = () => handler.Handle(new ListarUsuariosQuery(), default);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}

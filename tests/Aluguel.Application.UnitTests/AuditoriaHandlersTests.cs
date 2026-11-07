using Aluguel.Application.Auditoria.ListarAuditoria;
using Aluguel.Application.Imoveis.CriarImovel;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Auditoria;
using Aluguel.Domain.Imoveis;
using FluentAssertions;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class AuditoriaHandlersTests
{
    private static readonly Guid Tenant = Guid.Parse("2e8c8210-56a6-4078-9cde-f61fd6142ae8");
    private static readonly Guid ClienteA = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");

    [Fact]
    public async Task Criar_imovel_registra_auditoria_de_cadastro()
    {
        var repo = new FakeImovelRepository();
        var auditoria = new FakeAuditoriaService();
        var handler = new CriarImovelCommandHandler(repo, new FakeCurrentTenant(Tenant), auditoria);

        var dto = await handler.Handle(
            new CriarImovelCommand(ClienteA, "Casa Verde", TipoImovel.Residencial, null, null, null),
            default);

        auditoria.Registros.Should().ContainSingle();
        var log = auditoria.Registros[0];
        log.Acao.Should().Be(AcaoAuditoria.Cadastro);
        log.Tabela.Should().Be(TabelaAuditoria.Imovel);
        log.RegistroId.Should().Be(dto.Id);
        log.ClienteId.Should().Be(ClienteA);
        log.ValoresDepois.Should().NotBeNull();
        log.Resumo.Should().Contain("Casa Verde");
    }

    [Fact]
    public async Task Listar_analista_filtra_pelo_cliente_do_contexto()
    {
        var auditoria = new FakeAuditoriaService();
        await auditoria.RegistrarAsync(AcaoAuditoria.Cadastro, TabelaAuditoria.Imovel, Guid.NewGuid(), ClienteA,
            null, new { nome = "A" });
        await auditoria.RegistrarAsync(AcaoAuditoria.Cadastro, TabelaAuditoria.Imovel, Guid.NewGuid(), Guid.NewGuid(),
            null, new { nome = "B" });

        var analista = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false, nome: "Ana");
        var handler = new ListarAuditoriaQueryHandler(auditoria, analista);

        var pagina = await handler.Handle(new ListarAuditoriaQuery(Take: 50), default);

        pagina.Total.Should().Be(1);
        pagina.Itens.Should().ContainSingle(i => i.ClienteId == ClienteA);
    }

    [Fact]
    public async Task Listar_admin_pode_ver_todos_ou_filtrar_cliente()
    {
        var auditoria = new FakeAuditoriaService();
        var outro = Guid.NewGuid();
        await auditoria.RegistrarAsync(AcaoAuditoria.Cadastro, TabelaAuditoria.Cliente, ClienteA, ClienteA, null, new { });
        await auditoria.RegistrarAsync(AcaoAuditoria.Alteracao, TabelaAuditoria.Usuario, Guid.NewGuid(), outro, null, new { });

        var admin = new FakeCurrentUser(Guid.NewGuid(), null, ehAdministrador: true, nome: "Admin");
        var handler = new ListarAuditoriaQueryHandler(auditoria, admin);

        var todos = await handler.Handle(new ListarAuditoriaQuery(), default);
        todos.Total.Should().Be(2);

        var filtrado = await handler.Handle(new ListarAuditoriaQuery(ClienteId: ClienteA), default);
        filtrado.Total.Should().Be(1);
        filtrado.Itens[0].ClienteId.Should().Be(ClienteA);
    }
}

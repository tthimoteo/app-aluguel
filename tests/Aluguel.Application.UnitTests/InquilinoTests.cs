using Aluguel.Application.Inquilinos.CriarInquilino;
using Aluguel.Application.Inquilinos.RemoverInquilino;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Clientes;
using Aluguel.Domain.Inquilinos;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class InquilinoTests
{
    private static readonly Guid Tenant = Guid.Parse("2e8c8210-56a6-4078-9cde-f61fd6142ae8");
    private static readonly Guid ClienteA = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");

    private static CriarInquilinoCommand Comando(
        TipoPessoa tipo = TipoPessoa.PF, string documento = "390.533.447-05", string nome = "João da Silva") =>
        new(ClienteA, tipo, nome, documento, null, null, "joao@demo.local", null);

    [Fact]
    public async Task Validator_pf_com_cpf_valido_passa()
    {
        var result = await new CriarInquilinoCommandValidator().TestValidateAsync(Comando());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validator_pf_com_cpf_invalido_falha()
    {
        var result = await new CriarInquilinoCommandValidator().TestValidateAsync(Comando(documento: "111.111.111-11"));
        result.ShouldHaveValidationErrorFor(x => x.Documento);
    }

    [Fact]
    public async Task Validator_pj_com_cnpj_valido_passa()
    {
        var result = await new CriarInquilinoCommandValidator()
            .TestValidateAsync(Comando(TipoPessoa.PJ, "11.222.333/0001-81", "Imobiliária XPTO"));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validator_pj_com_cnpj_invalido_falha()
    {
        var result = await new CriarInquilinoCommandValidator()
            .TestValidateAsync(Comando(TipoPessoa.PJ, "00.000.000/0000-00", "Imobiliária XPTO"));
        result.ShouldHaveValidationErrorFor(x => x.Documento);
    }

    [Fact]
    public async Task Criar_normaliza_documento_e_define_tenant()
    {
        var repo = new FakeInquilinoRepository();
        var handler = new CriarInquilinoCommandHandler(repo, new FakeCurrentTenant(Tenant));

        var dto = await handler.Handle(Comando(), default);

        dto.TenantId.Should().Be(Tenant);
        dto.ClienteId.Should().Be(ClienteA);
        dto.Documento.Should().Be("39053344705");
    }

    [Fact]
    public async Task Remover_com_contrato_ativo_lanca()
    {
        var repo = new FakeInquilinoRepository();
        var inquilino = new Inquilino(Tenant, ClienteA, TipoPessoa.PF, "João", "39053344705");
        repo.Itens.Add(inquilino);
        repo.InquilinosComContratoAtivo.Add(inquilino.Id);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new RemoverInquilinoCommandHandler(repo, gestor);

        await handler.Invoking(h => h.Handle(new RemoverInquilinoCommand(inquilino.Id), default))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Remover_no_escopo_sem_dependentes_ok()
    {
        var repo = new FakeInquilinoRepository();
        var inquilino = new Inquilino(Tenant, ClienteA, TipoPessoa.PF, "João", "39053344705");
        repo.Itens.Add(inquilino);

        var gestor = new FakeCurrentUser(Guid.NewGuid(), ClienteA, ehAdministrador: false);
        var handler = new RemoverInquilinoCommandHandler(repo, gestor);

        (await handler.Handle(new RemoverInquilinoCommand(inquilino.Id), default)).Should().BeTrue();
    }
}

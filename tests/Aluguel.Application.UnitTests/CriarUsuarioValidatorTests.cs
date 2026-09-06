using Aluguel.Application.Usuarios.CriarUsuario;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Usuarios;
using FluentValidation.TestHelper;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class CriarUsuarioValidatorTests
{
    private static readonly Guid Cliente = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");

    private static CriarUsuarioCommand Comando(
        string email = "novo@demo.local",
        string senha = "Senha@12345",
        PerfilUsuario perfil = PerfilUsuario.Analista,
        string? cpf = null) =>
        new(Cliente, "Fulano de Tal", email, cpf, "11999998888", perfil, senha);

    [Fact]
    public async Task Analista_valido_passa()
    {
        var result = await new CriarUsuarioCommandValidator(new FakeUsuarioService())
            .TestValidateAsync(Comando());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Email_invalido_falha()
    {
        var result = await new CriarUsuarioCommandValidator(new FakeUsuarioService())
            .TestValidateAsync(Comando(email: "isso-nao-e-email"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Senha_curta_falha()
    {
        var result = await new CriarUsuarioCommandValidator(new FakeUsuarioService())
            .TestValidateAsync(Comando(senha: "curta"));
        result.ShouldHaveValidationErrorFor(x => x.Senha);
    }

    [Fact]
    public async Task Perfil_administrador_falha()
    {
        var result = await new CriarUsuarioCommandValidator(new FakeUsuarioService())
            .TestValidateAsync(Comando(perfil: PerfilUsuario.Administrador));
        result.ShouldHaveValidationErrorFor(x => x.Perfil);
    }

    [Fact]
    public async Task Email_em_uso_falha()
    {
        var service = new FakeUsuarioService();
        service.EmailsEmUso.Add("novo@demo.local");
        var result = await new CriarUsuarioCommandValidator(service).TestValidateAsync(Comando());
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Cpf_invalido_falha()
    {
        var result = await new CriarUsuarioCommandValidator(new FakeUsuarioService())
            .TestValidateAsync(Comando(cpf: "111.111.111-11"));
        result.ShouldHaveValidationErrorFor(x => x.Cpf);
    }

    [Fact]
    public async Task Cpf_duplicado_no_cliente_falha()
    {
        var service = new FakeUsuarioService();
        service.CpfsEmUso.Add((Cliente, "39053344705"));
        var result = await new CriarUsuarioCommandValidator(service)
            .TestValidateAsync(Comando(cpf: "390.533.447-05"));
        result.ShouldHaveValidationErrorFor(x => x.Cpf);
    }

    [Fact]
    public async Task Limite_do_plano_atingido_falha()
    {
        var service = new FakeUsuarioService { PodeAdicionar = false };
        var result = await new CriarUsuarioCommandValidator(service).TestValidateAsync(Comando());
        result.ShouldHaveValidationErrorFor(x => x.ClienteId);
    }
}

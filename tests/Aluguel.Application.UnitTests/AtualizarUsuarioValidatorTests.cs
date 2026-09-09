using Aluguel.Application.Usuarios.AtualizarUsuario;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Usuarios;
using FluentValidation.TestHelper;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class AtualizarUsuarioValidatorTests
{
    private static readonly Guid UsuarioId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Cliente = Guid.Parse("6f9619ff-8b86-d011-b42d-00cf4fc964ff");

    private static AtualizarUsuarioCommand Comando(string email = "novo@demo.local") =>
        new(UsuarioId, Cliente, "Fulano de Tal", email, "11999998888",
            PerfilUsuario.Analista, StatusUsuario.Ativo);

    [Fact]
    public async Task Email_valido_passa()
    {
        var result = await new AtualizarUsuarioCommandValidator(new FakeUsuarioService())
            .TestValidateAsync(Comando());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Email_invalido_falha()
    {
        var result = await new AtualizarUsuarioCommandValidator(new FakeUsuarioService())
            .TestValidateAsync(Comando(email: "isso-nao-e-email"));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Email_em_uso_falha()
    {
        var service = new FakeUsuarioService();
        service.EmailsEmUso.Add("ocupado@demo.local");

        var result = await new AtualizarUsuarioCommandValidator(service)
            .TestValidateAsync(Comando(email: "ocupado@demo.local"));

        result.ShouldHaveValidationErrorFor(x => x.Email);
    }
}

using Aluguel.Application.Clientes.CriarCliente;
using Aluguel.Application.UnitTests.Fakes;
using Aluguel.Domain.Clientes;
using FluentValidation.TestHelper;
using Xunit;

namespace Aluguel.Application.UnitTests;

public class CriarClienteValidatorTests
{
    private static CriarClienteCommandValidator CriarValidator(FakeClienteRepository? repo = null) =>
        new(repo ?? new FakeClienteRepository());

    private static CriarClienteCommand PessoaFisica(string? cpf = "390.533.447-05", string? nome = "João") =>
        new(TipoPessoa.PF, null, nome, cpf, null, null, null, null, null, null, "11999998888", "joao@x.com", null);

    private static CriarClienteCommand PessoaJuridica(string? cnpj = "11.222.333/0001-81", string? razao = "ACME LTDA") =>
        new(TipoPessoa.PJ, null, null, null, null, razao, "ACME", cnpj, null, null, null, null, null);

    [Fact]
    public async Task PF_valida_passa()
    {
        var result = await CriarValidator().TestValidateAsync(PessoaFisica());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task PF_sem_nome_falha()
    {
        var result = await CriarValidator().TestValidateAsync(PessoaFisica(nome: ""));
        result.ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public async Task PF_cpf_invalido_falha()
    {
        var result = await CriarValidator().TestValidateAsync(PessoaFisica(cpf: "111.111.111-11"));
        result.ShouldHaveValidationErrorFor(x => x.Cpf);
    }

    [Fact]
    public async Task PF_cpf_duplicado_falha()
    {
        var repo = new FakeClienteRepository();
        repo.Adicionar(Cliente.CriarPessoaFisica(Guid.NewGuid(), null, "Maria", "39053344705", null, null, null, null));

        var result = await CriarValidator(repo).TestValidateAsync(PessoaFisica());
        result.ShouldHaveValidationErrorFor(x => x.Cpf);
    }

    [Fact]
    public async Task PJ_valida_passa()
    {
        var result = await CriarValidator().TestValidateAsync(PessoaJuridica());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task PJ_sem_razao_social_falha()
    {
        var result = await CriarValidator().TestValidateAsync(PessoaJuridica(razao: ""));
        result.ShouldHaveValidationErrorFor(x => x.RazaoSocial);
    }

    [Fact]
    public async Task PJ_cnpj_invalido_falha()
    {
        var result = await CriarValidator().TestValidateAsync(PessoaJuridica(cnpj: "11.222.333/0001-00"));
        result.ShouldHaveValidationErrorFor(x => x.Cnpj);
    }
}

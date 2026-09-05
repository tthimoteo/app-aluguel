using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Validacoes;
using Aluguel.Domain.Clientes;
using FluentValidation;

namespace Aluguel.Application.Clientes.CriarCliente;

public sealed class CriarClienteCommandValidator : AbstractValidator<CriarClienteCommand>
{
    public CriarClienteCommandValidator(IClienteRepository repositorio)
    {
        RuleFor(x => x.TipoPessoa).IsInEnum();

        // ----- Pessoa Física -----
        When(x => x.TipoPessoa == TipoPessoa.PF, () =>
        {
            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("Nome é obrigatório para Pessoa Física.")
                .MaximumLength(150);

            RuleFor(x => x.Cpf)
                .NotEmpty().WithMessage("CPF é obrigatório para Pessoa Física.")
                .Must(cpf => Documento.CpfValido(Documento.SomenteDigitos(cpf)))
                .WithMessage("CPF inválido.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.Cpf)
                        .MustAsync(async (cpf, ct) =>
                            !await repositorio.CpfEmUsoAsync(Documento.SomenteDigitos(cpf)!, null, ct))
                        .WithMessage("Já existe um cliente com este CPF.");
                });

            RuleFor(x => x.DataNascimento)
                .Must(d => d is null || d.Value < DateOnly.FromDateTime(DateTime.UtcNow))
                .WithMessage("Data de nascimento deve estar no passado.");
        });

        // ----- Pessoa Jurídica -----
        When(x => x.TipoPessoa == TipoPessoa.PJ, () =>
        {
            RuleFor(x => x.RazaoSocial)
                .NotEmpty().WithMessage("Razão social é obrigatória para Pessoa Jurídica.")
                .MaximumLength(150);

            RuleFor(x => x.NomeFantasia).MaximumLength(150);
            RuleFor(x => x.InscricaoMunicipal).MaximumLength(30);
            RuleFor(x => x.CnaePrincipal).MaximumLength(10);

            RuleFor(x => x.Cnpj)
                .NotEmpty().WithMessage("CNPJ é obrigatório para Pessoa Jurídica.")
                .Must(cnpj => Documento.CnpjValido(Documento.SomenteDigitos(cnpj)))
                .WithMessage("CNPJ inválido.")
                .DependentRules(() =>
                {
                    RuleFor(x => x.Cnpj)
                        .MustAsync(async (cnpj, ct) =>
                            !await repositorio.CnpjEmUsoAsync(Documento.SomenteDigitos(cnpj)!, null, ct))
                        .WithMessage("Já existe um cliente com este CNPJ.");
                });
        });

        // ----- Comuns -----
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Telefone).MaximumLength(20);
        RuleFor(x => x.Endereco!.Uf).Length(2).When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Uf));
        RuleFor(x => x.Endereco!.Cep)
            .Must(cep => Documento.SomenteDigitos(cep)!.Length == 8)
            .When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Cep))
            .WithMessage("CEP deve conter 8 dígitos.");
    }
}

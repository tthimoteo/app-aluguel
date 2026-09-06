using Aluguel.Application.Common.Validacoes;
using Aluguel.Domain.Clientes;
using FluentValidation;

namespace Aluguel.Application.Inquilinos.CriarInquilino;

public sealed class CriarInquilinoCommandValidator : AbstractValidator<CriarInquilinoCommand>
{
    public CriarInquilinoCommandValidator()
    {
        RuleFor(x => x.ClienteId).NotEmpty().WithMessage("Cliente é obrigatório.");
        RuleFor(x => x.TipoPessoa).IsInEnum();

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome/Razão social é obrigatório.")
            .MaximumLength(150);

        When(x => x.TipoPessoa == TipoPessoa.PF, () =>
        {
            RuleFor(x => x.Documento)
                .NotEmpty().WithMessage("CPF é obrigatório para Pessoa Física.")
                .Must(doc => Documento.CpfValido(Documento.SomenteDigitos(doc)))
                .WithMessage("CPF inválido.");
        });

        When(x => x.TipoPessoa == TipoPessoa.PJ, () =>
        {
            RuleFor(x => x.Documento)
                .NotEmpty().WithMessage("CNPJ é obrigatório para Pessoa Jurídica.")
                .Must(doc => Documento.CnpjValido(Documento.SomenteDigitos(doc)))
                .WithMessage("CNPJ inválido.");
        });

        RuleFor(x => x.InscricaoMunicipal).MaximumLength(30);
        RuleFor(x => x.Telefone).MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Endereco!.Uf).Length(2).When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Uf));
        RuleFor(x => x.Endereco!.Cep)
            .Must(cep => Documento.SomenteDigitos(cep)!.Length == 8)
            .When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Cep))
            .WithMessage("CEP deve conter 8 dígitos.");
    }
}

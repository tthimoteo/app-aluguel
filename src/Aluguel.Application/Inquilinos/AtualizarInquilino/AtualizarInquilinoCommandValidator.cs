using Aluguel.Application.Common.Validacoes;
using FluentValidation;

namespace Aluguel.Application.Inquilinos.AtualizarInquilino;

public sealed class AtualizarInquilinoCommandValidator : AbstractValidator<AtualizarInquilinoCommand>
{
    public AtualizarInquilinoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome/Razão social é obrigatório.")
            .MaximumLength(150);

        RuleFor(x => x.InscricaoMunicipal).MaximumLength(30);
        RuleFor(x => x.Telefone).MaximumLength(20);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email));
        RuleFor(x => x.Status).IsInEnum().WithMessage("Status inválido.");

        RuleFor(x => x.Endereco!.Uf).Length(2).When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Uf));
        RuleFor(x => x.Endereco!.Cep)
            .Must(cep => Documento.SomenteDigitos(cep)!.Length == 8)
            .When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Cep))
            .WithMessage("CEP deve conter 8 dígitos.");
    }
}

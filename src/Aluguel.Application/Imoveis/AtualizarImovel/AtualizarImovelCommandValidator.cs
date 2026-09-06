using Aluguel.Application.Common.Validacoes;
using FluentValidation;

namespace Aluguel.Application.Imoveis.AtualizarImovel;

public sealed class AtualizarImovelCommandValidator : AbstractValidator<AtualizarImovelCommand>
{
    public AtualizarImovelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150);

        RuleFor(x => x.Tipo).IsInEnum().WithMessage("Tipo de imóvel inválido.");
        RuleFor(x => x.Status).IsInEnum().WithMessage("Status inválido.");

        RuleFor(x => x.NumeroIptu).MaximumLength(30);
        RuleFor(x => x.NumeroMatricula).MaximumLength(30);

        RuleFor(x => x.Endereco!.Uf).Length(2).When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Uf));
        RuleFor(x => x.Endereco!.Cep)
            .Must(cep => Documento.SomenteDigitos(cep)!.Length == 8)
            .When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Cep))
            .WithMessage("CEP deve conter 8 dígitos.");
    }
}

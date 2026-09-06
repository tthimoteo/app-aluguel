using Aluguel.Application.Abstractions;
using Aluguel.Application.Common.Validacoes;
using FluentValidation;

namespace Aluguel.Application.Imoveis.CriarImovel;

public sealed class CriarImovelCommandValidator : AbstractValidator<CriarImovelCommand>
{
    public CriarImovelCommandValidator(IImovelRepository repositorio)
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("Cliente é obrigatório.")
            .DependentRules(() =>
            {
                RuleFor(x => x.ClienteId)
                    .MustAsync((clienteId, ct) => repositorio.PodeAdicionarImovelAsync(clienteId, ct))
                    .WithName("Plano")
                    .WithMessage("Limite de imóveis do plano atingido.");
            });

        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("Nome é obrigatório.")
            .MaximumLength(150);

        RuleFor(x => x.Tipo).IsInEnum().WithMessage("Tipo de imóvel inválido.");

        RuleFor(x => x.NumeroIptu).MaximumLength(30);
        RuleFor(x => x.NumeroMatricula).MaximumLength(30);

        RuleFor(x => x.Endereco!.Uf).Length(2).When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Uf));
        RuleFor(x => x.Endereco!.Cep)
            .Must(cep => Documento.SomenteDigitos(cep)!.Length == 8)
            .When(x => !string.IsNullOrWhiteSpace(x.Endereco?.Cep))
            .WithMessage("CEP deve conter 8 dígitos.");
    }
}

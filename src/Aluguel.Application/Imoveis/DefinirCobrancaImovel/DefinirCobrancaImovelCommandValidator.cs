using System.Text.RegularExpressions;
using FluentValidation;

namespace Aluguel.Application.Imoveis.DefinirCobrancaImovel;

public sealed class DefinirCobrancaImovelCommandValidator : AbstractValidator<DefinirCobrancaImovelCommand>
{
    private static readonly Regex CompetenciaRegex = new(
        @"^(0[1-9]|1[0-2])/\d{4}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public DefinirCobrancaImovelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.CompetenciaInicial)
            .Must(c => string.IsNullOrWhiteSpace(c) || CompetenciaRegex.IsMatch(c.Trim()))
            .WithMessage("Competência inicial deve estar no formato MM/AAAA.");

        RuleFor(x => x.DiaVencimentoCobranca)
            .InclusiveBetween(1, 31)
            .When(x => x.DiaVencimentoCobranca is not null)
            .WithMessage("Dia de vencimento da cobrança deve estar entre 1 e 31.");

        RuleFor(x => x.DespesasCondominiais)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DespesasCondominiais is not null)
            .WithMessage("Despesas condominiais não podem ser negativas.");

        RuleFor(x => x.ValorIptu)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ValorIptu is not null)
            .WithMessage("Valor do IPTU não pode ser negativo.");
    }
}

using FluentValidation;

namespace Aluguel.Application.Contratos.AtualizarContrato;

public sealed class AtualizarContratoCommandValidator : AbstractValidator<AtualizarContratoCommand>
{
    public AtualizarContratoCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.DiaVencimento)
            .InclusiveBetween(1, 31).WithMessage("Dia de vencimento deve estar entre 1 e 31.");

        RuleFor(x => x.ValorAluguel)
            .GreaterThan(0).WithMessage("Valor do aluguel deve ser positivo.");

        RuleFor(x => x.JurosAtrasoPct)
            .InclusiveBetween(0, 999.999m).When(x => x.JurosAtrasoPct is not null)
            .WithMessage("Juros de atraso inválido.");

        RuleFor(x => x.MultaAtrasoPct)
            .InclusiveBetween(0, 999.999m).When(x => x.MultaAtrasoPct is not null)
            .WithMessage("Multa de atraso inválida.");

        RuleFor(x => x.AnexoPath).MaximumLength(500);
    }
}

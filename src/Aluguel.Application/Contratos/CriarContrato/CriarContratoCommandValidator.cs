using FluentValidation;

namespace Aluguel.Application.Contratos.CriarContrato;

public sealed class CriarContratoCommandValidator : AbstractValidator<CriarContratoCommand>
{
    public CriarContratoCommandValidator()
    {
        RuleFor(x => x.ImovelId).NotEmpty().WithMessage("Imóvel é obrigatório.");
        RuleFor(x => x.InquilinoId).NotEmpty().WithMessage("Inquilino é obrigatório.");

        RuleFor(x => x.NumeroContrato)
            .NotEmpty().WithMessage("Número do contrato é obrigatório.")
            .MaximumLength(40);

        RuleFor(x => x.DataInicio)
            .Must(d => d != default).WithMessage("Data de início é obrigatória.");

        RuleFor(x => x.DiaVencimento)
            .InclusiveBetween(1, 31).WithMessage("Dia de vencimento deve estar entre 1 e 31.");

        RuleFor(x => x.ValorAluguel)
            .GreaterThan(0).WithMessage("Valor do aluguel deve ser positivo.");

        RuleFor(x => x.DataFimPrevista)
            .Must((cmd, fim) => fim is null || fim >= cmd.DataInicio)
            .WithMessage("Data fim prevista não pode ser anterior à data de início.");

        RuleFor(x => x.JurosAtrasoPct)
            .InclusiveBetween(0, 999.999m).When(x => x.JurosAtrasoPct is not null)
            .WithMessage("Juros de atraso inválido.");

        RuleFor(x => x.MultaAtrasoPct)
            .InclusiveBetween(0, 999.999m).When(x => x.MultaAtrasoPct is not null)
            .WithMessage("Multa de atraso inválida.");
    }
}

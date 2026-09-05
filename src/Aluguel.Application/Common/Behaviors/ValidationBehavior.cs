using FluentValidation;
using MediatR;

namespace Aluguel.Application.Common.Behaviors;

/// <summary>Pipeline do MediatR que executa os validadores FluentValidation antes de cada handler.</summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var contexto = new ValidationContext<TRequest>(request);
            var resultados = await Task.WhenAll(
                validators.Select(v => v.ValidateAsync(contexto, cancellationToken)));

            var falhas = resultados.SelectMany(r => r.Errors).Where(f => f is not null).ToList();
            if (falhas.Count != 0)
                throw new ValidationException(falhas);
        }

        return await next();
    }
}

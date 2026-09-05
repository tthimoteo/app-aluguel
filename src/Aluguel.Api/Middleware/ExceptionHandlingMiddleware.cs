using FluentValidation;

namespace Aluguel.Api.Middleware;

/// <summary>Traduz exceções de validação/regra de negócio em respostas ProblemDetails (400).</summary>
public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            var erros = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

            await Results.ValidationProblem(erros, title: "Falha de validação")
                .ExecuteAsync(context);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            logger.LogWarning(ex, "Requisição inválida");
            await Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest,
                title: "Requisição inválida").ExecuteAsync(context);
        }
    }
}

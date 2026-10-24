using FluentValidation;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Abstractions.Behaviors;

/// <summary>
/// Valida el formato de la entrada con FluentValidation antes de llegar al handler.
/// Si hay errores devuelve un <see cref="ValidationError"/> (400) sin ejecutar el caso de uso.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResult> :
    ICommandPipelineBehavior<TRequest, TResult>,
    IQueryPipelineBehavior<TRequest, TResult>
    where TResult : Result
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResult> HandleAsync(TRequest request, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var results = await Task.WhenAll(_validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var errors = results
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => Error.Validation(failure.ErrorMessage, failure.PropertyName))
            .Distinct()
            .ToArray();

        if (errors.Length == 0)
        {
            return await next();
        }

        return ResultFactory.Failure<TResult>(new ValidationError(errors));
    }
}

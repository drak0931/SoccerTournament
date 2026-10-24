using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Abstractions.Messaging;

/// <summary>
/// Siguiente paso del pipeline (otro comportamiento o el handler final).
/// </summary>
public delegate Task<TResult> RequestHandlerDelegate<TResult>()
    where TResult : Result;

/// <summary>
/// Comportamiento transversal que envuelve la ejecución de un command.
/// </summary>
public interface ICommandPipelineBehavior<in TCommand, TResult>
    where TResult : Result
{
    Task<TResult> HandleAsync(TCommand command, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken);
}

/// <summary>
/// Comportamiento transversal que envuelve la ejecución de una consulta.
/// Es un contrato distinto al de commands para que ningún comportamiento de escritura se aplique a lecturas.
/// </summary>
public interface IQueryPipelineBehavior<in TQuery, TResult>
    where TResult : Result
{
    Task<TResult> HandleAsync(TQuery query, RequestHandlerDelegate<TResult> next, CancellationToken cancellationToken);
}

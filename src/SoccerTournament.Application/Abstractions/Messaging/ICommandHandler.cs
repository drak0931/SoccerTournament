using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Abstractions.Messaging;

/// <summary>
/// Handler de un command sin valor de retorno. Implementado con EF Core y confirmado mediante IUnitOfWork.
/// </summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

/// <summary>
/// Handler de un command que devuelve un valor. Implementado con EF Core y confirmado mediante IUnitOfWork.
/// </summary>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken);
}

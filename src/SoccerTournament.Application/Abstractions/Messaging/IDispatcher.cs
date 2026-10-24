using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Abstractions.Messaging;

/// <summary>
/// Punto de entrada único de la Api hacia los casos de uso. Separa explícitamente escritura (Send) de lectura (Query).
/// </summary>
public interface IDispatcher
{
    Task<Result> SendAsync(ICommand command, CancellationToken cancellationToken = default);

    Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);

    Task<Result<TResponse>> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
}

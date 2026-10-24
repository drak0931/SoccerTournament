using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Abstractions.Messaging;

/// <summary>
/// Consulta del lado de lectura (read side). No modifica estado.
/// </summary>
public interface IQuery<TResponse>
{
}

/// <summary>
/// Handler de una consulta. Usa exclusivamente servicios de lectura (Dapper); nunca repositorios EF ni IUnitOfWork.
/// </summary>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}

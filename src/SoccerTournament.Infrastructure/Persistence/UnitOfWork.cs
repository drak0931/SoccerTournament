using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Infrastructure.Persistence.DomainEvents;

namespace SoccerTournament.Infrastructure.Persistence;

/// <summary>
/// Implementación del Unit of Work sobre EF Core. Es el ÚNICO lugar donde se llama a SaveChanges.
/// <list type="number">
/// <item>Recolecta los eventos de dominio de los agregados rastreados y los agrega a dbo.domain_events.</item>
/// <item>Abre una transacción explícita (compatible con la estrategia de reintentos) y guarda todo.</item>
/// <item>Confirma; ante un conflicto de BD hace rollback y devuelve Result.Failure (409).</item>
/// <item>Tras el commit, despacha los eventos a sus handlers (logs con TraceId).</item>
/// </list>
/// </summary>
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _dbContext;
    private readonly DomainEventDispatcher _domainEventDispatcher;
    private readonly ILogger<UnitOfWork> _logger;

    public UnitOfWork(ApplicationDbContext dbContext, DomainEventDispatcher domainEventDispatcher, ILogger<UnitOfWork> logger)
    {
        _dbContext = dbContext;
        _domainEventDispatcher = domainEventDispatcher;
        _logger = logger;
    }

    public async Task<Result> CommitAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = _dbContext.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates.SelectMany(aggregate => aggregate.DomainEvents).ToList();

        // Los eventos se persisten en la MISMA transacción que los cambios que los originaron
        _dbContext.DomainEventRecords.AddRange(
            aggregates.SelectMany(aggregate => aggregate.DomainEvents.Select(domainEvent => DomainEventRecord.From(aggregate, domainEvent))));

        var commitResult = await SaveInTransactionAsync(cancellationToken);
        if (commitResult.IsFailure)
        {
            return commitResult;
        }

        aggregates.ForEach(aggregate => aggregate.ClearDomainEvents());

        await _domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);

        return Result.Success();
    }

    private async Task<Result> SaveInTransactionAsync(CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();

        try
        {
            var affectedRows = await strategy.ExecuteAsync(async token =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(token);

                // acceptAllChangesOnSuccess: false → si la estrategia reintenta, el ChangeTracker conserva los cambios
                var rows = await _dbContext.SaveChangesAsync(acceptAllChangesOnSuccess: false, token);
                await transaction.CommitAsync(token);

                return rows;
            }, cancellationToken);

            _dbContext.ChangeTracker.AcceptAllChanges();

            _logger.LogDebug("Transacción confirmada con {AffectedRows} filas afectadas", affectedRows);

            return Result.Success();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Límite de infraestructura: la excepción del proveedor se traduce a Result (la transacción ya hizo rollback)
            _logger.LogWarning(
                "Conflicto de concurrencia al confirmar cambios en {EntityTypes}",
                string.Join(", ", exception.Entries.Select(entry => entry.Metadata.ClrType.Name)));

            return SqlErrorTranslator.ConcurrencyConflict;
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException sqlException
                                                  && SqlErrorTranslator.Translate(sqlException.Number, sqlException.Message) is { } error)
        {
            _logger.LogWarning(
                "Conflicto de BD al confirmar cambios: SqlError {SqlErrorNumber} traducido a {ErrorCode}",
                sqlException.Number, error.Code);

            return error;
        }
    }
}

namespace SoccerTournament.Domain.Abstractions;

/// <summary>
/// Hecho relevante ocurrido en el dominio. Se registra en dbo.domain_events y se loguea al confirmar la transacción.
/// </summary>
public interface IDomainEvent
{
    Guid EventId { get; }

    DateTime OccurredOnUtc { get; }
}

/// <summary>
/// Base para eventos de dominio inmutables.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}

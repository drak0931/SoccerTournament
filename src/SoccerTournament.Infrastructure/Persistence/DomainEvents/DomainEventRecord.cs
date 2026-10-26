using System.Diagnostics;
using System.Text.Json;
using SoccerTournament.Application.Abstractions.Diagnostics;
using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Infrastructure.Persistence.DomainEvents;

/// <summary>
/// Registro persistido de un evento de dominio (dbo.domain_events), enlazado al TraceId del request.
/// </summary>
internal sealed class DomainEventRecord
{
    /// <summary>
    /// Clave de baggage de <see cref="Activity"/> donde el middleware de trazabilidad deja el CorrelationId.
    /// </summary>
    public const string CorrelationIdBaggageKey = CorrelationContext.BaggageKey;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    // Requerido por EF Core
    private DomainEventRecord()
    {
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; } = null!;

    public string AggregateType { get; private set; } = null!;

    public Guid AggregateId { get; private set; }

    public string Payload { get; private set; } = null!;

    public DateTime OccurredAt { get; private set; }

    public string? CorrelationId { get; private set; }

    public string? TraceId { get; private set; }

    public static DomainEventRecord From(AggregateRoot aggregate, IDomainEvent domainEvent)
    {
        var activity = Activity.Current;

        return new DomainEventRecord
        {
            Id = domainEvent.EventId,
            EventType = domainEvent.GetType().Name,
            AggregateType = aggregate.GetType().Name,
            AggregateId = aggregate.Id,
            // Se serializa con el tipo concreto para conservar todas las propiedades del evento
            Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
            OccurredAt = domainEvent.OccurredOnUtc,
            CorrelationId = activity?.GetBaggageItem(CorrelationIdBaggageKey),
            TraceId = activity?.TraceId.ToHexString()
        };
    }
}

using System.Diagnostics;
using System.Text.Json;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Matches.Events;
using SoccerTournament.Infrastructure.Persistence.DomainEvents;

namespace SoccerTournament.Infrastructure.Tests.Persistence;

public class DomainEventRecordTests
{
    [Fact]
    public void From_ShouldCaptureEventAggregateAndTrace()
    {
        var match = Match.Schedule(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc)).Value;
        var domainEvent = Assert.IsType<MatchScheduledDomainEvent>(Assert.Single(match.DomainEvents));

        using var activity = new Activity("test-request").Start();
        activity.SetBaggage(DomainEventRecord.CorrelationIdBaggageKey, "corr-123");

        var record = DomainEventRecord.From(match, domainEvent);

        Assert.Equal(domainEvent.EventId, record.Id);
        Assert.Equal(nameof(MatchScheduledDomainEvent), record.EventType);
        Assert.Equal(nameof(Match), record.AggregateType);
        Assert.Equal(match.Id, record.AggregateId);
        Assert.Equal(activity.TraceId.ToHexString(), record.TraceId);
        Assert.Equal(32, record.TraceId!.Length);
        Assert.Equal("corr-123", record.CorrelationId);
    }

    [Fact]
    public void From_ShouldSerializeConcreteEventPropertiesAsJson()
    {
        var match = Match.Schedule(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc)).Value;
        var domainEvent = (MatchScheduledDomainEvent)match.DomainEvents.Single();

        var record = DomainEventRecord.From(match, domainEvent);

        using var payload = JsonDocument.Parse(record.Payload);
        Assert.Equal(match.Id, payload.RootElement.GetProperty("matchId").GetGuid());
        Assert.Equal(domainEvent.HomeTeamId, payload.RootElement.GetProperty("homeTeamId").GetGuid());
    }
}

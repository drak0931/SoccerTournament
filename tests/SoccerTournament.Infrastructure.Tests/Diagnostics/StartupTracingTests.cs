using System.Diagnostics;
using SoccerTournament.Application.Abstractions.Diagnostics;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Infrastructure.Diagnostics;
using SoccerTournament.Infrastructure.Persistence.DomainEvents;

namespace SoccerTournament.Infrastructure.Tests.Diagnostics;

/// <summary>
/// El trabajo de arranque (migraciones y seed) ocurre fuera de un request HTTP; debe tener igualmente
/// una traza propia para que sus logs y eventos de dominio sean rastreables.
/// </summary>
public class StartupTracingTests
{
    [Fact]
    public void StartRootActivity_WithoutListeners_ShouldStillCreateW3CTrace()
    {
        using var activity = InfrastructureDiagnostics.StartRootActivity("DatabaseInitialization", "startup-test");

        Assert.Same(activity, Activity.Current);
        Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
        Assert.Matches("^[0-9a-f]{32}$", activity.TraceId.ToString());
        Assert.Equal("startup-test", CorrelationContext.Current);
    }

    [Fact]
    public void StartRootActivity_ShouldIgnoreAmbientParent()
    {
        using var ambient = new Activity("ambient").SetIdFormat(ActivityIdFormat.W3C).Start();

        using var root = InfrastructureDiagnostics.StartRootActivity("DatabaseInitialization", "startup-test");

        Assert.NotEqual(ambient.TraceId, root.TraceId);
    }

    [Fact]
    public void DomainEvent_RaisedDuringStartup_ShouldCarryStartupTraceAndCorrelation()
    {
        using var activity = InfrastructureDiagnostics.StartRootActivity("DatabaseInitialization", "startup-abc");
        var team = Team.Create("Club Arranque", "CAR", null).Value;

        var record = DomainEventRecord.From(team, team.DomainEvents.Single());

        Assert.Equal(activity.TraceId.ToString(), record.TraceId);
        Assert.Equal("startup-abc", record.CorrelationId);
    }
}

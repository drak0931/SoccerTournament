using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SoccerTournament.Application.Abstractions.Clock;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Infrastructure.Persistence.Auditing;

namespace SoccerTournament.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifica cómo EF Core rastrea los cambios del dominio antes de que el Unit of Work guarde.
/// </summary>
public class ChangeTrackingTests
{
    private static readonly DateTime Now = new(2026, 11, 1, 22, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RegisterResult_OnTrackedMatch_ShouldMarkNewGoalsAsAddedAndMatchAsModified()
    {
        // Las claves GUID se generan en el dominio: los goles nuevos deben insertarse, no actualizarse
        using var context = TestDbContextFactory.Create();
        var homeTeamId = Guid.NewGuid();
        var awayTeamId = Guid.NewGuid();
        var match = Match.Schedule(homeTeamId, awayTeamId, Now.AddHours(-2)).Value;
        context.Attach(match);

        match.RegisterResult(1, 0, new[] { new ScoredGoal(Guid.NewGuid(), homeTeamId, 30) }, Now);
        context.ChangeTracker.DetectChanges();

        Assert.Equal(EntityState.Modified, context.Entry(match).State);
        var goalEntry = Assert.Single(context.ChangeTracker.Entries<Goal>());
        Assert.Equal(EntityState.Added, goalEntry.State);
    }

    [Fact]
    public void AuditInterceptor_OnAddedEntity_ShouldSetCreatedBySystemUser()
    {
        using var context = TestDbContextFactory.Create();
        var team = Team.Create("Club Auditado", "CA", null).Value;
        context.Add(team);

        CreateInterceptor().ApplyAudit(context);

        Assert.Equal(SystemUser.Id, context.Entry(team).Property(AuditColumns.CreatedBy).CurrentValue);
        Assert.Null(context.Entry(team).Property(AuditColumns.ModifiedAt).CurrentValue);
    }

    [Fact]
    public void AuditInterceptor_OnModifiedEntity_ShouldSetModifiedAtAndModifiedBy()
    {
        using var context = TestDbContextFactory.Create();
        var team = Team.Create("Club Auditado", "CA", null).Value;
        context.Attach(team);

        team.Update("Club Auditado Renombrado", "CAR", null);
        context.ChangeTracker.DetectChanges();
        CreateInterceptor().ApplyAudit(context);

        var entry = context.Entry(team);
        Assert.Equal(EntityState.Modified, entry.State);
        Assert.Equal(Now, entry.Property(AuditColumns.ModifiedAt).CurrentValue);
        Assert.Equal(SystemUser.Id, entry.Property(AuditColumns.ModifiedBy).CurrentValue);
    }

    private static AuditInterceptor CreateInterceptor()
    {
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(Now);
        return new AuditInterceptor(dateTimeProvider);
    }
}

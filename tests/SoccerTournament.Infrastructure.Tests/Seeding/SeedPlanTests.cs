using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Standings;
using SoccerTournament.Infrastructure.Persistence.Seeding;

namespace SoccerTournament.Infrastructure.Tests.Seeding;

/// <summary>
/// Verifica que el seed cumple los requisitos de la prueba y deja la tabla lista para consultar.
/// </summary>
public class SeedPlanTests
{
    private static readonly DateTime SeedNow = new(2026, 10, 25, 15, 30, 0, DateTimeKind.Utc);

    private readonly SeedPlan _plan = SeedPlan.Build(SeedData.Teams, SeedData.Matches, SeedNow).Value;

    [Fact]
    public void Build_ShouldMeetRequiredQuantities()
    {
        Assert.Equal(4, _plan.Teams.Count);
        Assert.Equal(4 * 5, _plan.Players.Count);
        Assert.Equal(6, _plan.Matches.Count);
        Assert.Equal(3, _plan.ResultsCount);
        Assert.Equal(4, _plan.Standings.Count);
    }

    [Fact]
    public void Build_EachTeamShouldHaveFivePlayersWithUniqueJerseys()
    {
        foreach (var team in _plan.Teams)
        {
            var roster = _plan.Players.Where(player => player.TeamId == team.Id).ToList();

            Assert.Equal(5, roster.Count);
            Assert.Equal(roster.Count, roster.Select(player => player.JerseyNumber).Distinct().Count());
        }
    }

    [Fact]
    public void Build_ShouldScheduleSingleRoundRobin()
    {
        // 4 equipos todos contra todos = 6 cruces distintos, sin repetir
        var pairs = _plan.Matches
            .Select(match => string.Join("|", new[] { match.HomeTeamId, match.AwayTeamId }.OrderBy(id => id)))
            .ToList();

        Assert.Equal(6, pairs.Distinct().Count());
        Assert.All(_plan.Teams, team =>
            Assert.Equal(3, _plan.Matches.Count(match => match.IsParticipant(team.Id))));
    }

    [Fact]
    public void Build_ShouldLeavePlayedPendingAndFutureMatches()
    {
        var finished = _plan.Matches.Where(match => match.Status == MatchStatus.Finished).ToList();
        var scheduled = _plan.Matches.Where(match => match.Status == MatchStatus.Scheduled).ToList();

        Assert.All(finished, match => Assert.True(match.ScheduledAt < SeedNow));
        Assert.Single(scheduled, match => match.ScheduledAt < SeedNow); // pendiente de registrar desde el frontend
        Assert.Equal(2, scheduled.Count(match => match.ScheduledAt > SeedNow));
        Assert.All(_plan.Matches, match => Assert.Equal(DateTimeKind.Utc, match.ScheduledAt.Kind));
    }

    [Fact]
    public void Build_ShouldProduceExpectedStandings()
    {
        var table = _plan.Standings
            .OrderByDescending(standing => standing.Points)
            .ThenByDescending(standing => standing.GoalDifference)
            .ThenByDescending(standing => standing.GoalsFor)
            .Select(standing => (Team: ShortNameOf(standing.TeamId), standing.Points, standing.GoalDifference))
            .ToList();

        Assert.Equal(
            new[] { ("RMC", 4, 2), ("VOL", 3, -1), ("DLP", 1, 0), ("ACS", 0, -1) },
            table);

        Assert.All(_plan.Standings, standing =>
        {
            Assert.Equal(standing.Won + standing.Drawn + standing.Lost, standing.Played);
            Assert.Equal(standing.GoalsFor - standing.GoalsAgainst, standing.GoalDifference);
        });
    }

    [Fact]
    public void Build_GoalsShouldMatchScoresAndProduceTiedTopScorers()
    {
        var finished = _plan.Matches.Where(match => match.Status == MatchStatus.Finished).ToList();
        Assert.All(finished, match => Assert.Equal(match.HomeGoals + match.AwayGoals, match.Goals.Count));

        var goalsByPlayer = finished
            .SelectMany(match => match.Goals)
            .GroupBy(goal => goal.PlayerId)
            .Select(group => group.Count())
            .OrderByDescending(count => count)
            .ToList();

        Assert.Equal(7, goalsByPlayer.Sum());
        Assert.Equal(new[] { 2, 2 }, goalsByPlayer.Take(2));
    }

    [Fact]
    public void Build_ShouldRaiseDomainEventsForTraceability()
    {
        Assert.All(_plan.Teams, team => Assert.NotEmpty(team.DomainEvents));
        Assert.Equal(3, _plan.Matches.Count(match => match.DomainEvents.OfType<Domain.Matches.Events.MatchResultRegisteredDomainEvent>().Any()));
    }

    [Fact]
    public void Build_WithUnknownScorer_ShouldFailWithoutThrowing()
    {
        var brokenMatches = new[]
        {
            new SeedData.MatchSeed("VOL", "ACS", -1, new SeedData.ResultSeed(1, 0, new[] { new SeedData.GoalSeed("VOL", 99, 10) }))
        };

        var result = SeedPlan.Build(SeedData.Teams, brokenMatches, SeedNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Seed.UnknownScorer", result.ErrorCode);
    }

    private string ShortNameOf(Guid teamId) => _plan.Teams.Single(team => team.Id == teamId).ShortName;
}

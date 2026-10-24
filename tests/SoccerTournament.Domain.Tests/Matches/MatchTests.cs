using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Matches.Events;

namespace SoccerTournament.Domain.Tests.Matches;

public class MatchTests
{
    private static readonly Guid HomeTeamId = Guid.NewGuid();
    private static readonly Guid AwayTeamId = Guid.NewGuid();
    private static readonly Guid HomePlayerId = Guid.NewGuid();
    private static readonly Guid AwayPlayerId = Guid.NewGuid();
    private static readonly DateTime ScheduledAt = new(2026, 11, 1, 20, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime RegisteredAt = new(2026, 11, 1, 22, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Schedule_WithValidData_ShouldCreateScheduledMatchAndRaiseEvent()
    {
        var result = Match.Schedule(HomeTeamId, AwayTeamId, ScheduledAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Scheduled, result.Value.Status);
        Assert.Null(result.Value.HomeGoals);
        Assert.IsType<MatchScheduledDomainEvent>(Assert.Single(result.Value.DomainEvents));
    }

    [Fact]
    public void Schedule_WithSameTeams_ShouldFail()
    {
        var result = Match.Schedule(HomeTeamId, HomeTeamId, ScheduledAt);

        Assert.Equal(MatchErrors.SameTeams, result.Error);
    }

    [Fact]
    public void Schedule_WithoutDate_ShouldFail()
    {
        var result = Match.Schedule(HomeTeamId, AwayTeamId, default);

        Assert.Equal(MatchErrors.ScheduledAtRequired, result.Error);
    }

    [Fact]
    public void RegisterResult_WithConsistentGoals_ShouldFinishMatchAndRaiseEvent()
    {
        var match = CreateScheduledMatch();
        var goals = new[]
        {
            new ScoredGoal(HomePlayerId, HomeTeamId, 10),
            new ScoredGoal(HomePlayerId, HomeTeamId, 55),
            new ScoredGoal(AwayPlayerId, AwayTeamId, 80)
        };

        var result = match.RegisterResult(2, 1, goals, RegisteredAt);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Finished, match.Status);
        Assert.Equal((byte)2, match.HomeGoals);
        Assert.Equal((byte)1, match.AwayGoals);
        Assert.Equal(RegisteredAt, match.ResultRegisteredAt);
        Assert.Equal(3, match.Goals.Count);
        Assert.All(match.Goals, goal => Assert.Equal(match.Id, goal.MatchId));

        var domainEvent = Assert.IsType<MatchResultRegisteredDomainEvent>(Assert.Single(match.DomainEvents));
        Assert.Equal(2, domainEvent.HomeGoals);
        Assert.Equal(1, domainEvent.AwayGoals);
    }

    [Fact]
    public void RegisterResult_ScorelessDraw_ShouldSucceedWithoutGoals()
    {
        var match = CreateScheduledMatch();

        var result = match.RegisterResult(0, 0, Array.Empty<ScoredGoal>(), RegisteredAt);

        Assert.True(result.IsSuccess);
        Assert.Empty(match.Goals);
    }

    [Fact]
    public void RegisterResult_Twice_ShouldReturnConflict()
    {
        var match = CreateScheduledMatch();
        match.RegisterResult(0, 0, Array.Empty<ScoredGoal>(), RegisteredAt);

        var result = match.RegisterResult(1, 0, new[] { new ScoredGoal(HomePlayerId, HomeTeamId, 5) }, RegisteredAt);

        Assert.Equal(MatchErrors.ResultAlreadyRegistered, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal((byte)0, match.HomeGoals);
    }

    [Fact]
    public void RegisterResult_OnCancelledMatch_ShouldReturnConflict()
    {
        var match = CreateScheduledMatch();
        match.Cancel();

        var result = match.RegisterResult(0, 0, Array.Empty<ScoredGoal>(), RegisteredAt);

        Assert.Equal(MatchErrors.MatchCancelled, result.Error);
    }

    [Fact]
    public void RegisterResult_WhenGoalsDoNotMatchScore_ShouldFailAndNotModifyMatch()
    {
        var match = CreateScheduledMatch();

        var result = match.RegisterResult(2, 0, new[] { new ScoredGoal(HomePlayerId, HomeTeamId, 10) }, RegisteredAt);

        Assert.Equal(MatchErrors.GoalsDoNotMatchScore, result.Error);
        Assert.Equal(MatchStatus.Scheduled, match.Status);
        Assert.Empty(match.Goals);
        Assert.Empty(match.DomainEvents);
    }

    [Fact]
    public void RegisterResult_WithGoalFromTeamNotInMatch_ShouldFail()
    {
        var match = CreateScheduledMatch();
        var foreignTeamId = Guid.NewGuid();

        var result = match.RegisterResult(1, 0, new[] { new ScoredGoal(HomePlayerId, foreignTeamId, 10) }, RegisteredAt);

        var validationError = Assert.IsType<ValidationError>(result.Error);
        Assert.Contains(MatchErrors.GoalTeamNotInMatch, validationError.Errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(131)]
    public void RegisterResult_WithGoalMinuteOutOfRange_ShouldFail(int minute)
    {
        var match = CreateScheduledMatch();

        var result = match.RegisterResult(1, 0, new[] { new ScoredGoal(HomePlayerId, HomeTeamId, minute) }, RegisteredAt);

        Assert.Equal(MatchErrors.GoalMinuteOutOfRange, result.Error);
    }

    [Fact]
    public void RegisterResult_WithNegativeScore_ShouldFail()
    {
        var match = CreateScheduledMatch();

        var result = match.RegisterResult(-1, 0, Array.Empty<ScoredGoal>(), RegisteredAt);

        var validationError = Assert.IsType<ValidationError>(result.Error);
        Assert.Contains(MatchErrors.ScoreOutOfRange, validationError.Errors);
    }

    [Fact]
    public void CanRegisterResult_ShouldReflectStatusWithoutModifyingMatch()
    {
        var scheduled = CreateScheduledMatch();
        var cancelled = CreateScheduledMatch();
        cancelled.Cancel();

        Assert.True(scheduled.CanRegisterResult().IsSuccess);
        Assert.Equal(MatchErrors.MatchCancelled, cancelled.CanRegisterResult().Error);
        Assert.Equal(MatchStatus.Scheduled, scheduled.Status);
    }

    [Fact]
    public void Cancel_FinishedMatch_ShouldReturnConflict()
    {
        var match = CreateScheduledMatch();
        match.RegisterResult(0, 0, Array.Empty<ScoredGoal>(), RegisteredAt);

        var result = match.Cancel();

        Assert.Equal(MatchErrors.ResultAlreadyRegistered, result.Error);
        Assert.Equal(MatchStatus.Finished, match.Status);
    }

    private static Match CreateScheduledMatch()
    {
        var match = Match.Schedule(HomeTeamId, AwayTeamId, ScheduledAt).Value;
        match.ClearDomainEvents();
        return match;
    }
}

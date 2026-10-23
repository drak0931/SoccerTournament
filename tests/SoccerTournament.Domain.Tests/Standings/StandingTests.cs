using SoccerTournament.Domain.Standings;

namespace SoccerTournament.Domain.Tests.Standings;

public class StandingTests
{
    [Fact]
    public void CreateFor_ShouldStartWithZeroValues()
    {
        var teamId = Guid.NewGuid();

        var standing = Standing.CreateFor(teamId);

        Assert.Equal(teamId, standing.TeamId);
        Assert.Equal(0, standing.Played);
        Assert.Equal(0, standing.Points);
    }

    [Fact]
    public void ApplyResult_Win_ShouldAddThreePoints()
    {
        var standing = Standing.CreateFor(Guid.NewGuid());

        standing.ApplyResult(3, 1);

        Assert.Equal(1, standing.Played);
        Assert.Equal(1, standing.Won);
        Assert.Equal(Standing.PointsPerWin, standing.Points);
        Assert.Equal(2, standing.GoalDifference);
    }

    [Fact]
    public void ApplyResult_Draw_ShouldAddOnePoint()
    {
        var standing = Standing.CreateFor(Guid.NewGuid());

        standing.ApplyResult(2, 2);

        Assert.Equal(1, standing.Drawn);
        Assert.Equal(Standing.PointsPerDraw, standing.Points);
        Assert.Equal(0, standing.GoalDifference);
    }

    [Fact]
    public void ApplyResult_Loss_ShouldAddNoPoints()
    {
        var standing = Standing.CreateFor(Guid.NewGuid());

        standing.ApplyResult(0, 2);

        Assert.Equal(1, standing.Lost);
        Assert.Equal(0, standing.Points);
        Assert.Equal(-2, standing.GoalDifference);
    }

    [Fact]
    public void ApplyResult_SeveralMatches_ShouldKeepTableInvariants()
    {
        var standing = Standing.CreateFor(Guid.NewGuid());

        standing.ApplyResult(2, 0);
        standing.ApplyResult(1, 1);
        standing.ApplyResult(0, 3);

        // Mismas invariantes que ck_standings_played y ck_standings_goal_difference
        Assert.Equal(standing.Won + standing.Drawn + standing.Lost, standing.Played);
        Assert.Equal(standing.GoalsFor - standing.GoalsAgainst, standing.GoalDifference);
        Assert.Equal(4, standing.Points);
        Assert.Equal(3, standing.GoalsFor);
        Assert.Equal(4, standing.GoalsAgainst);
    }

    [Fact]
    public void ApplyResult_WithNegativeGoals_ShouldFailAndNotModify()
    {
        var standing = Standing.CreateFor(Guid.NewGuid());

        var result = standing.ApplyResult(-1, 0);

        Assert.Equal(StandingErrors.NegativeGoals, result.Error);
        Assert.Equal(0, standing.Played);
    }
}

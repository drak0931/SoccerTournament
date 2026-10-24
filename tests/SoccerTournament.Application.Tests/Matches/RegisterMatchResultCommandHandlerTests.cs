using NSubstitute;
using SoccerTournament.Application.Abstractions.Clock;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Application.Features.Matches.Commands;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Standings;

namespace SoccerTournament.Application.Tests.Matches;

public class RegisterMatchResultCommandHandlerTests
{
    private static readonly Guid HomeTeamId = Guid.NewGuid();
    private static readonly Guid AwayTeamId = Guid.NewGuid();
    private static readonly Guid HomePlayerId = Guid.NewGuid();
    private static readonly Guid AwayPlayerId = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 11, 1, 22, 0, 0, DateTimeKind.Utc);

    private readonly IMatchRepository _matchRepository = Substitute.For<IMatchRepository>();
    private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
    private readonly IStandingRepository _standingRepository = Substitute.For<IStandingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly RegisterMatchResultCommandHandler _handler;

    public RegisterMatchResultCommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _dateTimeProvider.UtcNow.Returns(Now);
        _playerRepository
            .GetTeamIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<Guid, Guid> { [HomePlayerId] = HomeTeamId, [AwayPlayerId] = AwayTeamId });

        _handler = new RegisterMatchResultCommandHandler(_matchRepository, _playerRepository, _standingRepository, _unitOfWork, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_WhenMatchDoesNotExist_ShouldReturnNotFound()
    {
        var result = await _handler.HandleAsync(new RegisterMatchResultCommand(Guid.NewGuid(), 0, 0, Array.Empty<GoalInput>()), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenResultAlreadyRegistered_ShouldReturnConflict()
    {
        var match = GivenScheduledMatch();
        match.RegisterResult(0, 0, Array.Empty<ScoredGoal>(), Now);

        var result = await _handler.HandleAsync(new RegisterMatchResultCommand(match.Id, 1, 0, new[] { HomeGoal(10) }), CancellationToken.None);

        Assert.Equal(MatchErrors.ResultAlreadyRegistered, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenScorerDoesNotBelongToTeam_ShouldFailWithoutModifyingMatch()
    {
        var match = GivenScheduledMatch();
        var wrongGoal = new GoalInput(AwayPlayerId, HomeTeamId, 15);

        var result = await _handler.HandleAsync(new RegisterMatchResultCommand(match.Id, 1, 0, new[] { wrongGoal }), CancellationToken.None);

        Assert.Equal(MatchErrors.GoalScorerNotInTeam, result.Error);
        Assert.Equal(MatchStatus.Scheduled, match.Status);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidResult_ShouldUpdateMatchAndBothStandingsInOneCommit()
    {
        var match = GivenScheduledMatch();
        var homeStanding = Standing.CreateFor(HomeTeamId);
        var awayStanding = Standing.CreateFor(AwayTeamId);
        _standingRepository
            .GetByTeamIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Standing> { homeStanding, awayStanding });

        var command = new RegisterMatchResultCommand(match.Id, 2, 1, new[] { HomeGoal(10), HomeGoal(70), new GoalInput(AwayPlayerId, AwayTeamId, 85) });
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Finished, result.Value.Status);
        Assert.Equal(Now, result.Value.ResultRegisteredAt);
        Assert.Equal(3, match.Goals.Count);

        Assert.Equal(Standing.PointsPerWin, homeStanding.Points);
        Assert.Equal(1, homeStanding.GoalDifference);
        Assert.Equal(0, awayStanding.Points);
        Assert.Equal(1, awayStanding.Lost);

        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStandingRowIsMissing_ShouldCreateItInSameTransaction()
    {
        var match = GivenScheduledMatch();
        _standingRepository
            .GetByTeamIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Standing>());

        var result = await _handler.HandleAsync(new RegisterMatchResultCommand(match.Id, 0, 0, Array.Empty<GoalInput>()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _standingRepository.Received(2).Add(Arg.Any<Standing>());
    }

    private Match GivenScheduledMatch()
    {
        var match = Match.Schedule(HomeTeamId, AwayTeamId, Now.AddHours(-2)).Value;
        _matchRepository.GetByIdAsync(match.Id, Arg.Any<CancellationToken>()).Returns(match);
        return match;
    }

    private static GoalInput HomeGoal(int minute) => new(HomePlayerId, HomeTeamId, minute);
}

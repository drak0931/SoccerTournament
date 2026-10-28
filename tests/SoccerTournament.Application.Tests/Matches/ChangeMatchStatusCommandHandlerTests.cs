using NSubstitute;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Application.Features.Matches.Commands;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches;

namespace SoccerTournament.Application.Tests.Matches;

public class ChangeMatchStatusCommandHandlerTests
{
    private readonly IMatchRepository _matchRepository = Substitute.For<IMatchRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ChangeMatchStatusCommandHandler _handler;

    public ChangeMatchStatusCommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new ChangeMatchStatusCommandHandler(_matchRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenMatchDoesNotExist_ShouldReturnNotFound()
    {
        var result = await _handler.HandleAsync(new ChangeMatchStatusCommand(Guid.NewGuid(), MatchStatus.Cancelled), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_Cancel_ShouldCommit()
    {
        var match = GivenScheduledMatch();

        var result = await _handler.HandleAsync(new ChangeMatchStatusCommand(match.Id, MatchStatus.Cancelled), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MatchStatus.Cancelled, match.Status);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnsupportedTransition_ShouldFailWithoutCommit()
    {
        var match = GivenScheduledMatch();

        var result = await _handler.HandleAsync(new ChangeMatchStatusCommand(match.Id, MatchStatus.Finished), CancellationToken.None);

        Assert.Equal(MatchErrors.UnsupportedStatusChange, result.Error);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private Match GivenScheduledMatch()
    {
        var match = Match.Schedule(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 11, 1, 1, 0, 0, DateTimeKind.Utc)).Value;
        _matchRepository.GetByIdAsync(match.Id, Arg.Any<CancellationToken>()).Returns(match);
        return match;
    }
}

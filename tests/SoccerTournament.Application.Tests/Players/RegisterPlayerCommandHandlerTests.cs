using NSubstitute;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Application.Features.Players.Commands;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Tests.Players;

public class RegisterPlayerCommandHandlerTests
{
    private readonly ITeamRepository _teamRepository = Substitute.For<ITeamRepository>();
    private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly RegisterPlayerCommandHandler _handler;

    public RegisterPlayerCommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
        _handler = new RegisterPlayerCommandHandler(_teamRepository, _playerRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenTeamDoesNotExist_ShouldReturnNotFound()
    {
        var result = await _handler.HandleAsync(
            new RegisterPlayerCommand(Guid.NewGuid(), "Ana", "López", 9, PlayerPosition.Forward), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Handle_WhenJerseyNumberIsTaken_ShouldReturnConflict()
    {
        var team = GivenTeam();
        _playerRepository.JerseyNumberExistsAsync(team.Id, 9, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _handler.HandleAsync(
            new RegisterPlayerCommand(team.Id, "Ana", "López", 9, PlayerPosition.Forward), CancellationToken.None);

        Assert.Equal(PlayerErrors.JerseyNumberTaken, result.Error);
        _playerRepository.DidNotReceive().Add(Arg.Any<Player>());
    }

    [Fact]
    public async Task Handle_ValidPlayer_ShouldAddAndCommit()
    {
        var team = GivenTeam();

        var result = await _handler.HandleAsync(
            new RegisterPlayerCommand(team.Id, "Ana", "López", 9, PlayerPosition.Forward), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(team.FullName, result.Value.TeamName);
        _playerRepository.Received(1).Add(Arg.Any<Player>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    private Team GivenTeam()
    {
        var team = Team.Create("Club Ejemplo", "CE", null).Value;
        _teamRepository.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        return team;
    }
}

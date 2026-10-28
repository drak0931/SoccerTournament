using NSubstitute;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Application.Features.Players.Queries;
using SoccerTournament.Application.Features.Teams.Queries;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Players;

namespace SoccerTournament.Application.Tests.Players;

public class GetTeamPlayersQueryHandlerTests
{
    private static readonly Guid TeamId = Guid.NewGuid();

    private readonly ITeamReadService _teamReadService = Substitute.For<ITeamReadService>();
    private readonly IPlayerReadService _playerReadService = Substitute.For<IPlayerReadService>();
    private readonly GetTeamPlayersQueryHandler _handler;

    public GetTeamPlayersQueryHandlerTests()
    {
        _handler = new GetTeamPlayersQueryHandler(_teamReadService, _playerReadService);
    }

    [Fact]
    public async Task Handle_WhenTeamDoesNotExist_ShouldReturnNotFoundInsteadOfEmptyList()
    {
        var result = await _handler.HandleAsync(new GetTeamPlayersQuery { TeamId = TeamId }, CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Team.NotFound", result.ErrorCode);
        await _playerReadService.DidNotReceive().GetPagedAsync(Arg.Any<GetPlayersQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingTeam_ShouldDelegateFiltersPagingAndSort()
    {
        _teamReadService.GetByIdAsync(TeamId, Arg.Any<CancellationToken>()).Returns(new TeamResponse(TeamId, "Club", "CLB", null));
        var page = new PagedResult<PlayerResponse>(Array.Empty<PlayerResponse>(), 2, 5, 7);
        _playerReadService.GetPagedAsync(Arg.Any<GetPlayersQuery>(), Arg.Any<CancellationToken>()).Returns(page);

        var query = new GetTeamPlayersQuery
        {
            TeamId = TeamId,
            Name = "per",
            Position = PlayerPosition.Forward,
            PageNumber = 2,
            PageSize = 5,
            SortBy = "jerseyNumber",
            SortDirection = SortDirection.Desc
        };

        var result = await _handler.HandleAsync(query, CancellationToken.None);

        Assert.Same(page, result.Value);
        await _playerReadService.Received(1).GetPagedAsync(
            Arg.Is<GetPlayersQuery>(players =>
                players.TeamId == TeamId && players.Name == "per" && players.Position == PlayerPosition.Forward
                && players.PageNumber == 2 && players.PageSize == 5
                && players.SortBy == "jerseyNumber" && players.SortDirection == SortDirection.Desc),
            Arg.Any<CancellationToken>());
    }
}

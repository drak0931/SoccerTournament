using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoccerTournament.Application.Abstractions.Clock;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Standings;
using SoccerTournament.Domain.Teams;
using SoccerTournament.Infrastructure.Persistence.Seeding;

namespace SoccerTournament.Infrastructure.Tests.Seeding;

public class DatabaseSeederTests
{
    private readonly ITeamRepository _teamRepository = Substitute.For<ITeamRepository>();
    private readonly IPlayerRepository _playerRepository = Substitute.For<IPlayerRepository>();
    private readonly IMatchRepository _matchRepository = Substitute.For<IMatchRepository>();
    private readonly IStandingRepository _standingRepository = Substitute.For<IStandingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly DatabaseSeeder _seeder;

    public DatabaseSeederTests()
    {
        var dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(new DateTime(2026, 10, 25, 15, 30, 0, DateTimeKind.Utc));
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());

        _seeder = new DatabaseSeeder(
            _teamRepository, _playerRepository, _matchRepository, _standingRepository,
            _unitOfWork, dateTimeProvider, NullLogger<DatabaseSeeder>.Instance);
    }

    [Fact]
    public async Task Seed_WhenDatabaseAlreadyHasTeams_ShouldDoNothing()
    {
        _teamRepository.AnyAsync(Arg.Any<CancellationToken>()).Returns(true);

        var result = await _seeder.SeedAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        _teamRepository.DidNotReceive().Add(Arg.Any<Team>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Seed_WhenDatabaseIsEmpty_ShouldAddEverythingInASingleCommit()
    {
        var result = await _seeder.SeedAsync(CancellationToken.None);

        Assert.True(result.IsSuccess);
        _teamRepository.Received(4).Add(Arg.Any<Team>());
        _standingRepository.Received(4).Add(Arg.Any<Standing>());
        _playerRepository.Received(20).Add(Arg.Any<Player>());
        _matchRepository.Received(6).Add(Arg.Any<Match>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Seed_WhenCommitFails_ShouldReturnFailureWithoutThrowing()
    {
        var conflict = Error.Conflict("Duplicado.", "Team.FullNameAlreadyExists");
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(conflict));

        var result = await _seeder.SeedAsync(CancellationToken.None);

        Assert.Equal(conflict, result.Error);
    }
}

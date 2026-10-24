using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoccerTournament.Application.Abstractions;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Application.Features.Teams.Commands;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Standings;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Tests.Teams;

public class TeamCommandHandlerTests
{
    private readonly ITeamRepository _teamRepository = Substitute.For<ITeamRepository>();
    private readonly IStandingRepository _standingRepository = Substitute.For<IStandingRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();

    public TeamCommandHandlerTests()
    {
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    [Fact]
    public async Task Create_WithDuplicatedFullName_ShouldReturnConflictWithoutCommit()
    {
        _teamRepository.FullNameExistsAsync("Club Repetido", null, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateTeamCommandHandler(_teamRepository, _standingRepository, _unitOfWork);

        var result = await handler.HandleAsync(new CreateTeamCommand("Club Repetido", "CR", null), CancellationToken.None);

        Assert.Equal(TeamErrors.FullNameAlreadyExists, result.Error);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_WhenCommitFails_ShouldPropagateError()
    {
        var concurrencyConflict = Error.Conflict("Conflicto en BD.", "Database.UniqueViolation");
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Failure(concurrencyConflict));
        var handler = new CreateTeamCommandHandler(_teamRepository, _standingRepository, _unitOfWork);

        var result = await handler.HandleAsync(new CreateTeamCommand("Club Nuevo", "CN", null), CancellationToken.None);

        Assert.Equal(concurrencyConflict, result.Error);
    }

    [Fact]
    public async Task Create_ShouldAddTeamAndStandingInSameUnitOfWork()
    {
        var handler = new CreateTeamCommandHandler(_teamRepository, _standingRepository, _unitOfWork);

        var result = await handler.HandleAsync(new CreateTeamCommand("Club Nuevo", "CN", null), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _teamRepository.Received(1).Add(Arg.Is<Team>(team => team.Id == result.Value.Id));
        _standingRepository.Received(1).Add(Arg.Is<Standing>(standing => standing.TeamId == result.Value.Id));
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WhenTeamDoesNotExist_ShouldReturnNotFound()
    {
        var handler = new UpdateTeamCommandHandler(_teamRepository, _unitOfWork);

        var result = await handler.HandleAsync(new UpdateTeamCommand(Guid.NewGuid(), "Club", "C", null), CancellationToken.None);

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Patch_ShouldOnlyChangeSentFields()
    {
        var team = Team.Create("Club Original", "CO", new DateTime(1930, 1, 1)).Value;
        _teamRepository.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        var handler = new PatchTeamCommandHandler(_teamRepository, _unitOfWork);

        var command = new PatchTeamCommand(team.Id, Optional<string>.Unspecified, Optional<string>.Of("CNV"), Optional<DateTime?>.Unspecified);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Club Original", result.Value.FullName);
        Assert.Equal("CNV", result.Value.ShortName);
        Assert.Equal(new DateTime(1930, 1, 1), result.Value.FoundedYear);
    }

    [Fact]
    public async Task Patch_WithExplicitNullFoundedYear_ShouldClearIt()
    {
        var team = Team.Create("Club Original", "CO", new DateTime(1930, 1, 1)).Value;
        _teamRepository.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        var handler = new PatchTeamCommandHandler(_teamRepository, _unitOfWork);

        var command = new PatchTeamCommand(team.Id, Optional<string>.Unspecified, Optional<string>.Unspecified, Optional<DateTime?>.Of(null));
        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.FoundedYear);
    }

    [Fact]
    public async Task Delete_WhenTeamDoesNotExist_ShouldSucceedIdempotently()
    {
        var handler = new DeleteTeamCommandHandler(_teamRepository, _unitOfWork, NullLogger<DeleteTeamCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteTeamCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_WhenTeamHasDependencies_ShouldReturnConflict()
    {
        var team = Team.Create("Club Con Jugadores", "CCJ", null).Value;
        _teamRepository.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        _teamRepository.HasDependenciesAsync(team.Id, Arg.Any<CancellationToken>()).Returns(true);
        var handler = new DeleteTeamCommandHandler(_teamRepository, _unitOfWork, NullLogger<DeleteTeamCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteTeamCommand(team.Id), CancellationToken.None);

        Assert.Equal(TeamErrors.HasDependencies, result.Error);
        _teamRepository.DidNotReceive().Remove(Arg.Any<Team>());
    }

    [Fact]
    public async Task Delete_ExistingTeamWithoutDependencies_ShouldRemoveAndCommit()
    {
        var team = Team.Create("Club Libre", "CL", null).Value;
        _teamRepository.GetByIdAsync(team.Id, Arg.Any<CancellationToken>()).Returns(team);
        var handler = new DeleteTeamCommandHandler(_teamRepository, _unitOfWork, NullLogger<DeleteTeamCommandHandler>.Instance);

        var result = await handler.HandleAsync(new DeleteTeamCommand(team.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        _teamRepository.Received(1).Remove(team);
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}

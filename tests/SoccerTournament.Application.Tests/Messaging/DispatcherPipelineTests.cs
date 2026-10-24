using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SoccerTournament.Application.Abstractions.Clock;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Application.Features.Teams.Commands;
using SoccerTournament.Application.Features.Teams.Queries;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Standings;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Tests.Messaging;

/// <summary>
/// Prueba el dispatcher de punta a punta con el contenedor DI real: resolución de handlers,
/// pipeline de validación y que una entrada inválida nunca llegue al handler ni a la BD.
/// </summary>
public class DispatcherPipelineTests
{
    private readonly ITeamRepository _teamRepository = Substitute.For<ITeamRepository>();
    private readonly IStandingRepository _standingRepository = Substitute.For<IStandingRepository>();
    private readonly ITeamReadService _teamReadService = Substitute.For<ITeamReadService>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IDispatcher _dispatcher;

    public DispatcherPipelineTests()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        // Dobles de prueba para los contratos que implementará Infrastructure
        services.AddSingleton(_teamRepository);
        services.AddSingleton(_standingRepository);
        services.AddSingleton(_teamReadService);
        services.AddSingleton(_unitOfWork);
        services.AddSingleton(Substitute.For<IPlayerRepository>());
        services.AddSingleton(Substitute.For<IMatchRepository>());
        services.AddSingleton(Substitute.For<IDateTimeProvider>());

        _dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();
        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Result.Success());
    }

    [Fact]
    public async Task SendAsync_ValidCommand_ShouldReachHandlerAndCommit()
    {
        var result = await _dispatcher.SendAsync(new CreateTeamCommand("Club Atlético Ejemplo", "CAE", null));

        Assert.True(result.IsSuccess);
        Assert.Equal("Club Atlético Ejemplo", result.Value.FullName);
        _teamRepository.Received(1).Add(Arg.Any<Team>());
        _standingRepository.Received(1).Add(Arg.Any<Standing>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_InvalidCommand_ShouldReturnValidationErrorWithoutReachingHandler()
    {
        var result = await _dispatcher.SendAsync(new CreateTeamCommand("", "", null));

        Assert.True(result.IsFailure);
        var validationError = Assert.IsType<ValidationError>(result.Error);
        Assert.Equal(ErrorType.Validation, validationError.Type);
        Assert.Equal(2, validationError.Errors.Count);
        _teamRepository.DidNotReceive().Add(Arg.Any<Team>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_CommandWithoutResponse_ShouldUseVoidPipeline()
    {
        var result = await _dispatcher.SendAsync(new DeleteTeamCommand(Guid.Empty));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task QueryAsync_ValidQuery_ShouldDelegateToReadService()
    {
        var expected = new PagedResult<TeamResponse>(Array.Empty<TeamResponse>(), 1, 10, 0);
        _teamReadService.GetPagedAsync(Arg.Any<GetTeamsQuery>(), Arg.Any<CancellationToken>()).Returns(expected);

        var result = await _dispatcher.QueryAsync(new GetTeamsQuery { SortBy = "fullName", SortDirection = SortDirection.Desc });

        Assert.True(result.IsSuccess);
        Assert.Same(expected, result.Value);
    }

    [Theory]
    [InlineData(0, 10, null)]
    [InlineData(1, 101, null)]
    [InlineData(1, 10, "full_name; DROP TABLE teams")]
    public async Task QueryAsync_InvalidPagingOrSort_ShouldFailWithoutHittingDatabase(int pageNumber, int pageSize, string? sortBy)
    {
        var result = await _dispatcher.QueryAsync(new GetTeamsQuery { PageNumber = pageNumber, PageSize = pageSize, SortBy = sortBy });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _teamReadService.DidNotReceive().GetPagedAsync(Arg.Any<GetTeamsQuery>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task QueryAsync_NotFound_ShouldReturnNotFoundError()
    {
        var teamId = Guid.NewGuid();
        _teamReadService.GetByIdAsync(teamId, Arg.Any<CancellationToken>()).Returns((TeamResponse?)null);

        var result = await _dispatcher.QueryAsync(new GetTeamByIdQuery(teamId));

        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        Assert.Equal("Team.NotFound", result.ErrorCode);
    }
}

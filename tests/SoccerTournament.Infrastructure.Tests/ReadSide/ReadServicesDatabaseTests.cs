using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Features.Matches.Queries;
using SoccerTournament.Application.Features.Players.Queries;
using SoccerTournament.Application.Features.Scorers.Queries;
using SoccerTournament.Application.Features.Standings.Queries;
using SoccerTournament.Application.Features.Teams.Queries;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Players;
using SoccerTournament.Infrastructure.Persistence;
using SoccerTournament.Infrastructure.ReadSide;

namespace SoccerTournament.Infrastructure.Tests.ReadSide;

/// <summary>
/// Ejecuta el SQL real de cada servicio de lectura contra SQL Server para validar sintaxis, nombres de
/// columnas y mapeo de tipos. Solo SELECT: no modifica datos. Opcional (ver <see cref="DatabaseFactAttribute"/>).
/// </summary>
public class ReadServicesDatabaseTests
{
    private readonly SqlConnectionFactory _connectionFactory = new(
        new SqlConnectionStringProvider(Environment.GetEnvironmentVariable(DatabaseFactAttribute.ConnectionStringVariable) ?? string.Empty));

    [DatabaseFact]
    public async Task Teams_AllSortFieldsAndFilters_ShouldExecute()
    {
        var service = new TeamReadService(_connectionFactory);

        foreach (var sortBy in GetTeamsQuery.SortableFields.Append(null))
        {
            var page = await service.GetPagedAsync(
                new GetTeamsQuery { SortBy = sortBy, SortDirection = SortDirection.Desc, Name = "a", FoundedFrom = 1900, FoundedTo = 2000 },
                CancellationToken.None);

            AssertConsistentPage(page);
        }

        Assert.Null(await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [DatabaseFact]
    public async Task Players_AllSortFieldsAndFilters_ShouldExecute()
    {
        var service = new PlayerReadService(_connectionFactory);

        foreach (var sortBy in GetPlayersQuery.SortableFields.Append(null))
        {
            var page = await service.GetPagedAsync(
                new GetPlayersQuery { SortBy = sortBy, TeamId = Guid.NewGuid(), Name = "a", Position = PlayerPosition.Forward },
                CancellationToken.None);

            AssertConsistentPage(page);
        }

        AssertConsistentPage(await service.GetPagedAsync(new GetPlayersQuery(), CancellationToken.None));
        Assert.Null(await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [DatabaseFact]
    public async Task Matches_AllSortFieldsAndFilters_ShouldExecute()
    {
        var service = new MatchReadService(_connectionFactory);

        foreach (var sortBy in GetMatchesQuery.SortableFields.Append(null))
        {
            var page = await service.GetPagedAsync(
                new GetMatchesQuery
                {
                    SortBy = sortBy,
                    DateFrom = new DateTime(2026, 1, 1),
                    DateTo = new DateTime(2026, 12, 31),
                    TeamId = Guid.NewGuid(),
                    Status = MatchStatus.Finished
                },
                CancellationToken.None);

            AssertConsistentPage(page);
        }

        AssertConsistentPage(await service.GetPagedAsync(new GetMatchesQuery { DateTo = new DateTime(2026, 12, 31, 18, 30, 0) }, CancellationToken.None));
        Assert.Null(await service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [DatabaseFact]
    public async Task Standings_AllSortFields_ShouldExecute()
    {
        var service = new StandingReadService(_connectionFactory);

        foreach (var sortBy in GetStandingsQuery.SortableFields.Append(null))
        {
            AssertConsistentPage(await service.GetPagedAsync(new GetStandingsQuery { SortBy = sortBy }, CancellationToken.None));
        }

        AssertConsistentPage(await service.GetPagedAsync(new GetStandingsQuery { TeamName = "a" }, CancellationToken.None));
    }

    [DatabaseFact]
    public async Task TopScorers_AllSortFieldsAndFilters_ShouldExecute()
    {
        var service = new ScorerReadService(_connectionFactory);

        foreach (var sortBy in GetTopScorersQuery.SortableFields.Append(null))
        {
            AssertConsistentPage(await service.GetPagedAsync(new GetTopScorersQuery { SortBy = sortBy }, CancellationToken.None));
        }

        AssertConsistentPage(await service.GetPagedAsync(new GetTopScorersQuery { TeamId = Guid.NewGuid() }, CancellationToken.None));
    }

    [DatabaseFact]
    public async Task PageBeyondLastPage_ShouldReturnEmptyDataWithRealTotal()
    {
        var service = new TeamReadService(_connectionFactory);

        var page = await service.GetPagedAsync(new GetTeamsQuery { PageNumber = 10_000, PageSize = 100 }, CancellationToken.None);

        Assert.Empty(page.Data);
        Assert.Equal(10_000, page.PageNumber);
        Assert.True(page.TotalRecords >= 0);
    }

    private static void AssertConsistentPage<T>(PagedResult<T> page)
    {
        Assert.True(page.Data.Count <= page.PageSize);
        Assert.True(page.TotalRecords >= page.Data.Count || page.PageNumber > 1);
        Assert.Equal((int)Math.Ceiling(page.TotalRecords / (double)page.PageSize), page.TotalPages);
    }
}

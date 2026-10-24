using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Features.Matches.Queries;
using SoccerTournament.Application.Features.Players.Queries;
using SoccerTournament.Application.Features.Scorers.Queries;
using SoccerTournament.Application.Features.Standings.Queries;
using SoccerTournament.Application.Features.Teams.Queries;

namespace SoccerTournament.Application.Abstractions.Persistence.Read;

// Servicios del lado de lectura. Infrastructure los implementa EXCLUSIVAMENTE con Dapper:
// SQL parametrizado, paginación OFFSET/FETCH en la BD y ORDER BY desde lista blanca.

public interface ITeamReadService
{
    Task<PagedResult<TeamResponse>> GetPagedAsync(GetTeamsQuery query, CancellationToken cancellationToken);

    Task<TeamResponse?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken);
}

public interface IPlayerReadService
{
    Task<PagedResult<PlayerResponse>> GetPagedAsync(GetPlayersQuery query, CancellationToken cancellationToken);

    Task<PlayerResponse?> GetByIdAsync(Guid playerId, CancellationToken cancellationToken);
}

public interface IMatchReadService
{
    Task<PagedResult<MatchResponse>> GetPagedAsync(GetMatchesQuery query, CancellationToken cancellationToken);

    Task<MatchDetailResponse?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken);
}

public interface IStandingReadService
{
    Task<PagedResult<StandingResponse>> GetPagedAsync(GetStandingsQuery query, CancellationToken cancellationToken);
}

public interface IScorerReadService
{
    Task<PagedResult<TopScorerResponse>> GetPagedAsync(GetTopScorersQuery query, CancellationToken cancellationToken);
}

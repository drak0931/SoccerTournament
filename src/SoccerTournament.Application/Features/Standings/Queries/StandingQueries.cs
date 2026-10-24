using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Features.Standings.Queries;

/// <summary>
/// Fila de la tabla de posiciones. <see cref="Position"/> siempre refleja el orden oficial
/// (puntos, diferencia de gol, goles a favor), aunque se ordene la página por otro campo.
/// </summary>
public sealed record StandingResponse(
    int Position,
    Guid TeamId,
    string TeamName,
    string TeamShortName,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GoalsFor,
    int GoalsAgainst,
    int GoalDifference,
    int Points);

/// <summary>
/// Tabla de posiciones automática. Orden por defecto: 1) Puntos 2) Diferencia de gol 3) Goles a favor.
/// </summary>
public sealed record GetStandingsQuery : PagedRequest, IQuery<PagedResult<StandingResponse>>
{
    public static readonly IReadOnlyCollection<string> SortableFields = new[] { "position", "points", "goalDifference", "goalsFor", "teamName" };

    public string? TeamName { get; init; }
}

internal sealed class GetStandingsQueryValidator : PagedRequestValidator<GetStandingsQuery>
{
    public GetStandingsQueryValidator()
        : base(GetStandingsQuery.SortableFields)
    {
    }
}

internal sealed class GetStandingsQueryHandler : IQueryHandler<GetStandingsQuery, PagedResult<StandingResponse>>
{
    private readonly IStandingReadService _standingReadService;

    public GetStandingsQueryHandler(IStandingReadService standingReadService)
    {
        _standingReadService = standingReadService;
    }

    public async Task<Result<PagedResult<StandingResponse>>> HandleAsync(GetStandingsQuery query, CancellationToken cancellationToken) =>
        await _standingReadService.GetPagedAsync(query, cancellationToken);
}

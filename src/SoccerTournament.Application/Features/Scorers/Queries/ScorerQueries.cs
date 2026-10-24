using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Features.Scorers.Queries;

/// <summary>
/// Goleador del torneo. <see cref="Rank"/> se calcula en la BD con DENSE_RANK por cantidad de goles.
/// </summary>
public sealed record TopScorerResponse(
    int Rank,
    Guid PlayerId,
    string PlayerName,
    Guid TeamId,
    string TeamName,
    int Goals);

/// <summary>
/// Tabla de goleadores, opcionalmente filtrada por equipo. Orden por defecto: goles descendente.
/// </summary>
public sealed record GetTopScorersQuery : PagedRequest, IQuery<PagedResult<TopScorerResponse>>
{
    public static readonly IReadOnlyCollection<string> SortableFields = new[] { "goals", "playerName", "teamName" };

    public Guid? TeamId { get; init; }
}

internal sealed class GetTopScorersQueryValidator : PagedRequestValidator<GetTopScorersQuery>
{
    public GetTopScorersQueryValidator()
        : base(GetTopScorersQuery.SortableFields)
    {
    }
}

internal sealed class GetTopScorersQueryHandler : IQueryHandler<GetTopScorersQuery, PagedResult<TopScorerResponse>>
{
    private readonly IScorerReadService _scorerReadService;

    public GetTopScorersQueryHandler(IScorerReadService scorerReadService)
    {
        _scorerReadService = scorerReadService;
    }

    public async Task<Result<PagedResult<TopScorerResponse>>> HandleAsync(GetTopScorersQuery query, CancellationToken cancellationToken) =>
        await _scorerReadService.GetPagedAsync(query, cancellationToken);
}

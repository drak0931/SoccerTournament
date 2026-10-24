using FluentValidation;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches;

namespace SoccerTournament.Application.Features.Matches.Queries;

public sealed record MatchResponse(
    Guid Id,
    Guid HomeTeamId,
    string HomeTeamName,
    Guid AwayTeamId,
    string AwayTeamName,
    DateTime ScheduledAt,
    MatchStatus Status,
    int? HomeGoals,
    int? AwayGoals,
    DateTime? ResultRegisteredAt);

public sealed record GoalResponse(Guid Id, Guid PlayerId, string PlayerName, Guid TeamId, int Minute);

public sealed record MatchDetailResponse(MatchResponse Match, IReadOnlyList<GoalResponse> Goals);

// ---------- Listado paginado ----------

/// <summary>
/// Lista partidos con filtros por rango de fechas, equipo (local o visitante) y estado.
/// </summary>
public sealed record GetMatchesQuery : PagedRequest, IQuery<PagedResult<MatchResponse>>
{
    public static readonly IReadOnlyCollection<string> SortableFields = new[] { "scheduledAt", "status", "createdAt" };

    public DateTime? DateFrom { get; init; }

    public DateTime? DateTo { get; init; }

    /// <summary>
    /// Partidos donde el equipo participa como local o visitante.
    /// </summary>
    public Guid? TeamId { get; init; }

    public MatchStatus? Status { get; init; }
}

internal sealed class GetMatchesQueryValidator : PagedRequestValidator<GetMatchesQuery>
{
    public GetMatchesQueryValidator()
        : base(GetMatchesQuery.SortableFields)
    {
        RuleFor(query => query.Status).IsInEnum().When(query => query.Status.HasValue);

        RuleFor(query => query)
            .Must(query => query.DateFrom is null || query.DateTo is null || query.DateFrom <= query.DateTo)
            .WithName(nameof(GetMatchesQuery.DateFrom))
            .WithMessage("La fecha inicial no puede ser mayor que la fecha final.");
    }
}

internal sealed class GetMatchesQueryHandler : IQueryHandler<GetMatchesQuery, PagedResult<MatchResponse>>
{
    private readonly IMatchReadService _matchReadService;

    public GetMatchesQueryHandler(IMatchReadService matchReadService)
    {
        _matchReadService = matchReadService;
    }

    public async Task<Result<PagedResult<MatchResponse>>> HandleAsync(GetMatchesQuery query, CancellationToken cancellationToken) =>
        await _matchReadService.GetPagedAsync(query, cancellationToken);
}

// ---------- Detalle (incluye goles) ----------

public sealed record GetMatchByIdQuery(Guid MatchId) : IQuery<MatchDetailResponse>;

internal sealed class GetMatchByIdQueryHandler : IQueryHandler<GetMatchByIdQuery, MatchDetailResponse>
{
    private readonly IMatchReadService _matchReadService;

    public GetMatchByIdQueryHandler(IMatchReadService matchReadService)
    {
        _matchReadService = matchReadService;
    }

    public async Task<Result<MatchDetailResponse>> HandleAsync(GetMatchByIdQuery query, CancellationToken cancellationToken)
    {
        var match = await _matchReadService.GetByIdAsync(query.MatchId, cancellationToken);

        return match is null ? MatchErrors.NotFound(query.MatchId) : match;
    }
}

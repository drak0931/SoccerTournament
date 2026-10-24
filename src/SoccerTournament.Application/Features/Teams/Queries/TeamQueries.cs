using FluentValidation;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Features.Teams.Queries;

public sealed record TeamResponse(Guid Id, string FullName, string ShortName, DateTime? FoundedYear);

// ---------- Listado paginado ----------

/// <summary>
/// Lista equipos con filtros, paginación y ordenamiento dinámico.
/// </summary>
public sealed record GetTeamsQuery : PagedRequest, IQuery<PagedResult<TeamResponse>>
{
    public static readonly IReadOnlyCollection<string> SortableFields = new[] { "fullName", "shortName", "foundedYear", "createdAt" };

    /// <summary>
    /// Búsqueda parcial por nombre completo o nombre corto.
    /// </summary>
    public string? Name { get; init; }

    public int? FoundedFrom { get; init; }

    public int? FoundedTo { get; init; }
}

internal sealed class GetTeamsQueryValidator : PagedRequestValidator<GetTeamsQuery>
{
    public GetTeamsQueryValidator()
        : base(GetTeamsQuery.SortableFields)
    {
        RuleFor(query => query.Name).MaximumLength(Team.FullNameMaxLength);

        RuleFor(query => query)
            .Must(query => query.FoundedFrom is null || query.FoundedTo is null || query.FoundedFrom <= query.FoundedTo)
            .WithName(nameof(GetTeamsQuery.FoundedFrom))
            .WithMessage("El año inicial de fundación no puede ser mayor que el año final.");
    }
}

internal sealed class GetTeamsQueryHandler : IQueryHandler<GetTeamsQuery, PagedResult<TeamResponse>>
{
    private readonly ITeamReadService _teamReadService;

    public GetTeamsQueryHandler(ITeamReadService teamReadService)
    {
        _teamReadService = teamReadService;
    }

    public async Task<Result<PagedResult<TeamResponse>>> HandleAsync(GetTeamsQuery query, CancellationToken cancellationToken) =>
        await _teamReadService.GetPagedAsync(query, cancellationToken);
}

// ---------- Detalle ----------

public sealed record GetTeamByIdQuery(Guid TeamId) : IQuery<TeamResponse>;

internal sealed class GetTeamByIdQueryHandler : IQueryHandler<GetTeamByIdQuery, TeamResponse>
{
    private readonly ITeamReadService _teamReadService;

    public GetTeamByIdQueryHandler(ITeamReadService teamReadService)
    {
        _teamReadService = teamReadService;
    }

    public async Task<Result<TeamResponse>> HandleAsync(GetTeamByIdQuery query, CancellationToken cancellationToken)
    {
        var team = await _teamReadService.GetByIdAsync(query.TeamId, cancellationToken);

        return team is null ? TeamErrors.NotFound(query.TeamId) : team;
    }
}

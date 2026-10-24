using FluentValidation;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Abstractions.Persistence.Read;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Players;

namespace SoccerTournament.Application.Features.Players.Queries;

public sealed record PlayerResponse(
    Guid Id,
    Guid TeamId,
    string TeamName,
    string? FirstName,
    string LastName,
    int JerseyNumber,
    PlayerPosition Position);

// ---------- Listado paginado ----------

/// <summary>
/// Lista jugadores (opcionalmente de un equipo) con filtros, paginación y ordenamiento.
/// </summary>
public sealed record GetPlayersQuery : PagedRequest, IQuery<PagedResult<PlayerResponse>>
{
    public static readonly IReadOnlyCollection<string> SortableFields = new[] { "lastName", "firstName", "jerseyNumber", "position", "teamName" };

    public Guid? TeamId { get; init; }

    /// <summary>
    /// Búsqueda parcial por nombre o apellido.
    /// </summary>
    public string? Name { get; init; }

    public PlayerPosition? Position { get; init; }
}

internal sealed class GetPlayersQueryValidator : PagedRequestValidator<GetPlayersQuery>
{
    public GetPlayersQueryValidator()
        : base(GetPlayersQuery.SortableFields)
    {
        RuleFor(query => query.Name).MaximumLength(Player.NameMaxLength);
        RuleFor(query => query.Position).IsInEnum().When(query => query.Position.HasValue);
    }
}

internal sealed class GetPlayersQueryHandler : IQueryHandler<GetPlayersQuery, PagedResult<PlayerResponse>>
{
    private readonly IPlayerReadService _playerReadService;

    public GetPlayersQueryHandler(IPlayerReadService playerReadService)
    {
        _playerReadService = playerReadService;
    }

    public async Task<Result<PagedResult<PlayerResponse>>> HandleAsync(GetPlayersQuery query, CancellationToken cancellationToken) =>
        await _playerReadService.GetPagedAsync(query, cancellationToken);
}

// ---------- Detalle ----------

public sealed record GetPlayerByIdQuery(Guid PlayerId) : IQuery<PlayerResponse>;

internal sealed class GetPlayerByIdQueryHandler : IQueryHandler<GetPlayerByIdQuery, PlayerResponse>
{
    private readonly IPlayerReadService _playerReadService;

    public GetPlayerByIdQueryHandler(IPlayerReadService playerReadService)
    {
        _playerReadService = playerReadService;
    }

    public async Task<Result<PlayerResponse>> HandleAsync(GetPlayerByIdQuery query, CancellationToken cancellationToken)
    {
        var player = await _playerReadService.GetByIdAsync(query.PlayerId, cancellationToken);

        return player is null ? PlayerErrors.NotFound(query.PlayerId) : player;
    }
}

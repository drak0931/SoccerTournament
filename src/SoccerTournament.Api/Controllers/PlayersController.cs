using Microsoft.AspNetCore.Mvc;
using SoccerTournament.Api.Common;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Features.Players.Queries;

namespace SoccerTournament.Api.Controllers;

/// <summary>
/// Integrantes de todos los equipos. El registro se hace por equipo: POST /api/teams/{teamId}/players.
/// </summary>
[Route("api/players")]
public sealed class PlayersController : ApiControllerBase
{
    public PlayersController(IDispatcher dispatcher)
        : base(dispatcher)
    {
    }

    /// <summary>Lista jugadores (filtros: teamId, name, position).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<PlayerResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPlayers([FromQuery] GetPlayersQuery query, CancellationToken cancellationToken) =>
        OkOrProblem(await Dispatcher.QueryAsync(query, cancellationToken));

    [HttpGet("{playerId:guid}")]
    [ProducesResponseType<PlayerResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlayerById(Guid playerId, CancellationToken cancellationToken) =>
        OkOrProblem(await Dispatcher.QueryAsync(new GetPlayerByIdQuery(playerId), cancellationToken));
}

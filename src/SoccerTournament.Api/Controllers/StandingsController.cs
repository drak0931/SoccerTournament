using Microsoft.AspNetCore.Mvc;
using SoccerTournament.Api.Common;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Features.Scorers.Queries;
using SoccerTournament.Application.Features.Standings.Queries;

namespace SoccerTournament.Api.Controllers;

/// <summary>
/// Tabla de posiciones automática (puntos → diferencia de gol → goles a favor).
/// </summary>
[Route("api/standings")]
public sealed class StandingsController : ApiControllerBase
{
    public StandingsController(IDispatcher dispatcher)
        : base(dispatcher)
    {
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<StandingResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetStandings([FromQuery] GetStandingsQuery query, CancellationToken cancellationToken) =>
        OkOrProblem(await Dispatcher.QueryAsync(query, cancellationToken));
}

/// <summary>
/// Tabla de goleadores (filtro: teamId).
/// </summary>
[Route("api/scorers")]
public sealed class ScorersController : ApiControllerBase
{
    public ScorersController(IDispatcher dispatcher)
        : base(dispatcher)
    {
    }

    [HttpGet]
    [ProducesResponseType<PagedResult<TopScorerResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTopScorers([FromQuery] GetTopScorersQuery query, CancellationToken cancellationToken) =>
        OkOrProblem(await Dispatcher.QueryAsync(query, cancellationToken));
}

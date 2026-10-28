using Microsoft.AspNetCore.Mvc;
using SoccerTournament.Api.Common;
using SoccerTournament.Api.Contracts;
using SoccerTournament.Api.Idempotency;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Features.Matches.Commands;
using SoccerTournament.Application.Features.Matches.Queries;

namespace SoccerTournament.Api.Controllers;

/// <summary>
/// Calendario de partidos y registro de resultados.
/// </summary>
[Route("api/matches")]
public sealed class MatchesController : ApiControllerBase
{
    public MatchesController(IDispatcher dispatcher)
        : base(dispatcher)
    {
    }

    /// <summary>Lista partidos (filtros: dateFrom, dateTo, teamId, status). Fechas en UTC.</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<MatchResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetMatches([FromQuery] GetMatchesQuery query, CancellationToken cancellationToken) =>
        OkOrProblem(await Dispatcher.QueryAsync(query, cancellationToken));

    /// <summary>Detalle del partido con sus goles.</summary>
    [HttpGet("{matchId:guid}")]
    [ProducesResponseType<MatchDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMatchById(Guid matchId, CancellationToken cancellationToken) =>
        OkOrProblem(await Dispatcher.QueryAsync(new GetMatchByIdQuery(matchId), cancellationToken));

    [HttpPost]
    [Idempotent]
    [ProducesResponseType<MatchResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ScheduleMatch([FromBody] ScheduleMatchRequest request, CancellationToken cancellationToken)
    {
        var result = await Dispatcher.SendAsync(
            new ScheduleMatchCommand(request.HomeTeamId, request.AwayTeamId, request.ScheduledAt.UtcDateTime), cancellationToken);

        return CreatedOrProblem(result, nameof(GetMatchById), null, match => new { matchId = match.Id });
    }

    /// <summary>Registra el resultado y los goleadores. Actualiza la tabla de posiciones en la misma transacción.</summary>
    [HttpPost("{matchId:guid}/result")]
    [Idempotent]
    [ProducesResponseType<MatchResultResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterResult(Guid matchId, [FromBody] RegisterMatchResultRequest request, CancellationToken cancellationToken)
    {
        var goals = (request.Goals ?? Array.Empty<GoalRequest>())
            .Select(goal => new GoalInput(goal.PlayerId, goal.TeamId, goal.Minute))
            .ToArray();

        var result = await Dispatcher.SendAsync(
            new RegisterMatchResultCommand(matchId, request.HomeGoals, request.AwayGoals, goals), cancellationToken);

        return CreatedOrProblem(result, nameof(GetMatchById), null, registered => new { matchId = registered.MatchId });
    }

    /// <summary>Cambio de estado del partido. Solo se admite {"status":"cancelled"}.</summary>
    [HttpPatch("{matchId:guid}")]
    [Consumes("application/merge-patch+json", "application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStatus(Guid matchId, [FromBody] UpdateMatchStatusRequest request, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Dispatcher.SendAsync(new ChangeMatchStatusCommand(matchId, request.Status), cancellationToken));
}

using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using SoccerTournament.Api.Common;
using SoccerTournament.Api.Contracts;
using SoccerTournament.Api.Idempotency;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Pagination;
using SoccerTournament.Application.Features.Players.Commands;
using SoccerTournament.Application.Features.Players.Queries;
using SoccerTournament.Application.Features.Teams.Commands;
using SoccerTournament.Application.Features.Teams.Queries;

namespace SoccerTournament.Api.Controllers;

/// <summary>
/// Equipos: CRUD completo con filtros, paginación y ordenamiento; registro de jugadores por equipo.
/// </summary>
[Route("api/teams")]
public sealed class TeamsController : ApiControllerBase
{
    public TeamsController(IDispatcher dispatcher)
        : base(dispatcher)
    {
    }

    /// <summary>Lista equipos (filtros: name, foundedFrom, foundedTo).</summary>
    [HttpGet]
    [ProducesResponseType<PagedResult<TeamResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTeams([FromQuery] GetTeamsQuery query, CancellationToken cancellationToken) =>
        OkOrProblem(await Dispatcher.QueryAsync(query, cancellationToken));

    [HttpGet("{teamId:guid}")]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTeamById(Guid teamId, CancellationToken cancellationToken) =>
        OkOrProblem(await Dispatcher.QueryAsync(new GetTeamByIdQuery(teamId), cancellationToken));

    [HttpPost]
    [Idempotent]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTeam([FromBody] CreateTeamRequest request, CancellationToken cancellationToken)
    {
        var result = await Dispatcher.SendAsync(new CreateTeamCommand(request.FullName, request.ShortName, request.FoundedYear), cancellationToken);

        return CreatedOrProblem(result, nameof(GetTeamById), null, team => new { teamId = team.Id });
    }

    /// <summary>Reemplazo completo del equipo (idempotente).</summary>
    [HttpPut("{teamId:guid}")]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReplaceTeam(Guid teamId, [FromBody] UpdateTeamRequest request, CancellationToken cancellationToken) =>
        OkOrProblem(await Dispatcher.SendAsync(
            new UpdateTeamCommand(teamId, request.FullName, request.ShortName, request.FoundedYear), cancellationToken));

    /// <summary>Modificación parcial (JSON Merge Patch): solo cambian los campos enviados.</summary>
    [HttpPatch("{teamId:guid}")]
    [Consumes("application/merge-patch+json", "application/json")]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PatchTeam(Guid teamId, [FromBody] JsonElement patch, CancellationToken cancellationToken)
    {
        var command = TeamMergePatch.ToCommand(teamId, patch);
        if (command.IsFailure)
        {
            return Problem(command.Error);
        }

        return OkOrProblem(await Dispatcher.SendAsync(command.Value, cancellationToken));
    }

    /// <summary>Elimina el equipo. Idempotente: devuelve 204 aunque ya no exista.</summary>
    [HttpDelete("{teamId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTeam(Guid teamId, CancellationToken cancellationToken) =>
        NoContentOrProblem(await Dispatcher.SendAsync(new DeleteTeamCommand(teamId), cancellationToken));

    /// <summary>Jugadores del equipo (filtros: name, position).</summary>
    [HttpGet("{teamId:guid}/players")]
    [ProducesResponseType<PagedResult<PlayerResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamPlayers(Guid teamId, [FromQuery] GetPlayersQuery query, CancellationToken cancellationToken) =>
        OkOrProblem(await Dispatcher.QueryAsync(query with { TeamId = teamId }, cancellationToken));

    [HttpPost("{teamId:guid}/players")]
    [Idempotent]
    [ProducesResponseType<PlayerResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegisterPlayer(Guid teamId, [FromBody] RegisterPlayerRequest request, CancellationToken cancellationToken)
    {
        var result = await Dispatcher.SendAsync(
            new RegisterPlayerCommand(teamId, request.FirstName, request.LastName, request.JerseyNumber, request.Position), cancellationToken);

        return CreatedOrProblem(result, nameof(PlayersController.GetPlayerById), "Players", player => new { playerId = player.Id });
    }
}

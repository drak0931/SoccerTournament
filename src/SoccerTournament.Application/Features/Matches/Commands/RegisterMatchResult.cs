using FluentValidation;
using SoccerTournament.Application.Abstractions.Clock;
using SoccerTournament.Application.Abstractions.Messaging;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Standings;

namespace SoccerTournament.Application.Features.Matches.Commands;

public sealed record GoalInput(Guid PlayerId, Guid TeamId, int Minute);

public sealed record MatchResultResponse(Guid MatchId, MatchStatus Status, int HomeGoals, int AwayGoals, DateTime ResultRegisteredAt);

/// <summary>
/// Registra el resultado final y los goleadores de un partido programado (POST /matches/{id}/result).
/// En una sola transacción actualiza el partido, sus goles y las filas de posiciones de ambos equipos.
/// </summary>
public sealed record RegisterMatchResultCommand(
    Guid MatchId,
    int HomeGoals,
    int AwayGoals,
    IReadOnlyCollection<GoalInput> Goals) : ICommand<MatchResultResponse>;

internal sealed class RegisterMatchResultCommandValidator : AbstractValidator<RegisterMatchResultCommand>
{
    public RegisterMatchResultCommandValidator()
    {
        RuleFor(command => command.MatchId).NotEmpty();
        RuleFor(command => command.HomeGoals).InclusiveBetween(0, Match.MaxGoalsPerTeam);
        RuleFor(command => command.AwayGoals).InclusiveBetween(0, Match.MaxGoalsPerTeam);
        RuleFor(command => command.Goals).NotNull();

        RuleForEach(command => command.Goals).ChildRules(goal =>
        {
            goal.RuleFor(item => item.PlayerId).NotEmpty();
            goal.RuleFor(item => item.TeamId).NotEmpty();
            goal.RuleFor(item => item.Minute).InclusiveBetween(Goal.MinMinute, Goal.MaxMinute);
        });
    }
}

internal sealed class RegisterMatchResultCommandHandler : ICommandHandler<RegisterMatchResultCommand, MatchResultResponse>
{
    private readonly IMatchRepository _matchRepository;
    private readonly IPlayerRepository _playerRepository;
    private readonly IStandingRepository _standingRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RegisterMatchResultCommandHandler(
        IMatchRepository matchRepository,
        IPlayerRepository playerRepository,
        IStandingRepository standingRepository,
        IUnitOfWork unitOfWork,
        IDateTimeProvider dateTimeProvider)
    {
        _matchRepository = matchRepository;
        _playerRepository = playerRepository;
        _standingRepository = standingRepository;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<MatchResultResponse>> HandleAsync(RegisterMatchResultCommand command, CancellationToken cancellationToken)
    {
        var match = await _matchRepository.GetByIdAsync(command.MatchId, cancellationToken);
        if (match is null)
        {
            return MatchErrors.NotFound(command.MatchId);
        }

        // Primero el estado (409) y luego los datos, sin modificar el agregado todavía
        var canRegister = match.CanRegisterResult();
        if (canRegister.IsFailure)
        {
            return canRegister.Error;
        }

        var scorersValidation = await ValidateScorersAsync(command.Goals, cancellationToken);
        if (scorersValidation.IsFailure)
        {
            return scorersValidation.Error;
        }

        var scoredGoals = command.Goals
            .Select(goal => new ScoredGoal(goal.PlayerId, goal.TeamId, goal.Minute))
            .ToArray();

        var registration = match.RegisterResult(command.HomeGoals, command.AwayGoals, scoredGoals, _dateTimeProvider.UtcNow);
        if (registration.IsFailure)
        {
            return registration.Error;
        }

        var standingsUpdate = await UpdateStandingsAsync(match, cancellationToken);
        if (standingsUpdate.IsFailure)
        {
            return standingsUpdate.Error;
        }

        // Partido + goles + posiciones de ambos equipos se confirman atómicamente
        var commit = await _unitOfWork.CommitAsync(cancellationToken);
        if (commit.IsFailure)
        {
            return commit.Error;
        }

        return new MatchResultResponse(match.Id, match.Status, match.HomeGoals!.Value, match.AwayGoals!.Value, match.ResultRegisteredAt!.Value);
    }

    // Cada goleador debe existir y pertenecer al equipo al que se acredita el gol
    private async Task<Result> ValidateScorersAsync(IReadOnlyCollection<GoalInput> goals, CancellationToken cancellationToken)
    {
        if (goals.Count == 0)
        {
            return Result.Success();
        }

        var playerIds = goals.Select(goal => goal.PlayerId).Distinct().ToArray();
        var playerTeams = await _playerRepository.GetTeamIdsAsync(playerIds, cancellationToken);

        var allScorersValid = goals.All(goal =>
            playerTeams.TryGetValue(goal.PlayerId, out var playerTeamId) && playerTeamId == goal.TeamId);

        return allScorersValid ? Result.Success() : MatchErrors.GoalScorerNotInTeam;
    }

    private async Task<Result> UpdateStandingsAsync(Match match, CancellationToken cancellationToken)
    {
        var standings = await _standingRepository.GetByTeamIdsAsync(new[] { match.HomeTeamId, match.AwayTeamId }, cancellationToken);

        var homeStanding = GetOrCreateStanding(standings, match.HomeTeamId);
        var awayStanding = GetOrCreateStanding(standings, match.AwayTeamId);

        var homeResult = homeStanding.ApplyResult(match.HomeGoals!.Value, match.AwayGoals!.Value);
        if (homeResult.IsFailure)
        {
            return homeResult;
        }

        return awayStanding.ApplyResult(match.AwayGoals!.Value, match.HomeGoals!.Value);
    }

    // Robustez: si un equipo no tuviera fila de posiciones (datos previos), se crea en la misma transacción
    private Standing GetOrCreateStanding(IReadOnlyList<Standing> standings, Guid teamId)
    {
        var standing = standings.FirstOrDefault(item => item.TeamId == teamId);
        if (standing is not null)
        {
            return standing;
        }

        standing = Standing.CreateFor(teamId);
        _standingRepository.Add(standing);
        return standing;
    }
}

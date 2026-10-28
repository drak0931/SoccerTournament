using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Matches.Events;

namespace SoccerTournament.Domain.Matches;

/// <summary>
/// Partido del calendario. Controla el ciclo de vida (programado → finalizado / cancelado)
/// y la consistencia entre el marcador y los goles registrados.
/// </summary>
public sealed class Match : AggregateRoot
{
    public const int MaxGoalsPerTeam = 50;

    private readonly List<Goal> _goals = new();

    private Match(Guid id, Guid homeTeamId, Guid awayTeamId, DateTime scheduledAt)
        : base(id)
    {
        HomeTeamId = homeTeamId;
        AwayTeamId = awayTeamId;
        ScheduledAt = scheduledAt;
        Status = MatchStatus.Scheduled;
    }

    // Requerido por EF Core
    private Match()
    {
    }

    public Guid HomeTeamId { get; private set; }

    public Guid AwayTeamId { get; private set; }

    /// <summary>
    /// Fecha y hora programada en UTC.
    /// </summary>
    public DateTime ScheduledAt { get; private set; }

    public MatchStatus Status { get; private set; }

    public byte? HomeGoals { get; private set; }

    public byte? AwayGoals { get; private set; }

    public DateTime? ResultRegisteredAt { get; private set; }

    public IReadOnlyCollection<Goal> Goals => _goals.AsReadOnly();

    /// <summary>
    /// Programa un nuevo partido. La existencia de los equipos se valida en Application.
    /// </summary>
    public static Result<Match> Schedule(Guid homeTeamId, Guid awayTeamId, DateTime scheduledAt)
    {
        var errors = new List<Error>();

        if (homeTeamId == Guid.Empty)
        {
            errors.Add(MatchErrors.HomeTeamRequired);
        }

        if (awayTeamId == Guid.Empty)
        {
            errors.Add(MatchErrors.AwayTeamRequired);
        }

        if (homeTeamId != Guid.Empty && homeTeamId == awayTeamId)
        {
            errors.Add(MatchErrors.SameTeams);
        }

        if (scheduledAt == default)
        {
            errors.Add(MatchErrors.ScheduledAtRequired);
        }

        if (errors.Count > 0)
        {
            return errors.Count == 1 ? errors[0] : new ValidationError(errors);
        }

        var match = new Match(Guid.NewGuid(), homeTeamId, awayTeamId, scheduledAt);

        match.Raise(new MatchScheduledDomainEvent(match.Id, homeTeamId, awayTeamId, scheduledAt));

        return match;
    }

    /// <summary>
    /// Registra el marcador final y los goles. Solo es posible una vez y sobre un partido programado.
    /// La pertenencia de cada goleador a su equipo se valida en Application (y en BD con fk_goals_player_team).
    /// </summary>
    public Result RegisterResult(int homeGoals, int awayGoals, IReadOnlyCollection<ScoredGoal> goals, DateTime registeredAt)
    {
        var canRegister = CanRegisterResult();
        if (canRegister.IsFailure)
        {
            return canRegister;
        }

        var validation = ValidateResult(homeGoals, awayGoals, goals);
        if (validation.IsFailure)
        {
            return validation;
        }

        HomeGoals = (byte)homeGoals;
        AwayGoals = (byte)awayGoals;
        ResultRegisteredAt = registeredAt;
        Status = MatchStatus.Finished;

        foreach (var scoredGoal in goals)
        {
            _goals.Add(Goal.Create(Id, scoredGoal));
        }

        Raise(new MatchResultRegisteredDomainEvent(Id, HomeTeamId, AwayTeamId, homeGoals, awayGoals));

        return Result.Success();
    }

    /// <summary>
    /// Cancela un partido programado. Un partido finalizado no puede cancelarse.
    /// </summary>
    public Result Cancel()
    {
        if (Status == MatchStatus.Finished)
        {
            return MatchErrors.ResultAlreadyRegistered;
        }

        if (Status == MatchStatus.Cancelled)
        {
            return MatchErrors.MatchCancelled;
        }

        Status = MatchStatus.Cancelled;

        Raise(new MatchCancelledDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Cambio de estado solicitado explícitamente (PATCH). Solo se admite la cancelación:
    /// un partido se finaliza registrando su resultado, nunca cambiando el estado directamente.
    /// </summary>
    public Result ChangeStatus(MatchStatus targetStatus) => targetStatus switch
    {
        MatchStatus.Cancelled => Cancel(),
        _ => MatchErrors.UnsupportedStatusChange
    };

    /// <summary>
    /// Indica si el partido admite registrar resultado (solo partidos programados). No modifica estado.
    /// </summary>
    public Result CanRegisterResult() => Status switch
    {
        MatchStatus.Finished => MatchErrors.ResultAlreadyRegistered,
        MatchStatus.Cancelled => MatchErrors.MatchCancelled,
        _ => Result.Success()
    };

    public bool IsParticipant(Guid teamId) => teamId == HomeTeamId || teamId == AwayTeamId;

    private Result ValidateResult(int homeGoals, int awayGoals, IReadOnlyCollection<ScoredGoal> goals)
    {
        var errors = new List<Error>();

        if (homeGoals is < 0 or > MaxGoalsPerTeam || awayGoals is < 0 or > MaxGoalsPerTeam)
        {
            errors.Add(MatchErrors.ScoreOutOfRange);
        }

        if (goals.Any(goal => !IsParticipant(goal.TeamId)))
        {
            errors.Add(MatchErrors.GoalTeamNotInMatch);
        }

        if (goals.Any(goal => goal.PlayerId == Guid.Empty))
        {
            errors.Add(MatchErrors.GoalPlayerRequired);
        }

        if (goals.Any(goal => goal.Minute is < Goal.MinMinute or > Goal.MaxMinute))
        {
            errors.Add(MatchErrors.GoalMinuteOutOfRange);
        }

        // El detalle de goles debe cuadrar con el marcador de cada equipo
        var homeScored = goals.Count(goal => goal.TeamId == HomeTeamId);
        var awayScored = goals.Count(goal => goal.TeamId == AwayTeamId);
        if (homeScored != homeGoals || awayScored != awayGoals)
        {
            errors.Add(MatchErrors.GoalsDoNotMatchScore);
        }

        return errors.Count switch
        {
            0 => Result.Success(),
            1 => errors[0],
            _ => new ValidationError(errors)
        };
    }
}

using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Domain.Matches;

/// <summary>
/// Catálogo de errores del agregado Match.
/// </summary>
public static class MatchErrors
{
    public static readonly Error HomeTeamRequired =
        Error.Validation("El equipo local es obligatorio.", "Match.HomeTeamRequired");

    public static readonly Error AwayTeamRequired =
        Error.Validation("El equipo visitante es obligatorio.", "Match.AwayTeamRequired");

    public static readonly Error SameTeams =
        Error.Validation("Un equipo no puede jugar contra sí mismo.", "Match.SameTeams");

    public static readonly Error ScheduledAtRequired =
        Error.Validation("La fecha del partido es obligatoria.", "Match.ScheduledAtRequired");

    public static readonly Error ScoreOutOfRange =
        Error.Validation($"El marcador de cada equipo debe estar entre 0 y {Match.MaxGoalsPerTeam}.", "Match.ScoreOutOfRange");

    public static readonly Error GoalTeamNotInMatch =
        Error.Validation("Todos los goles deben pertenecer al equipo local o al visitante.", "Match.GoalTeamNotInMatch");

    public static readonly Error GoalPlayerRequired =
        Error.Validation("Cada gol debe indicar el jugador que lo anotó.", "Match.GoalPlayerRequired");

    public static readonly Error GoalMinuteOutOfRange =
        Error.Validation($"El minuto del gol debe estar entre {Goal.MinMinute} y {Goal.MaxMinute}.", "Match.GoalMinuteOutOfRange");

    public static readonly Error GoalsDoNotMatchScore =
        Error.Validation("La cantidad de goles registrados no coincide con el marcador.", "Match.GoalsDoNotMatchScore");

    public static readonly Error GoalScorerNotInTeam =
        Error.Validation("Uno o más goleadores no pertenecen al equipo al que se acredita el gol.", "Match.GoalScorerNotInTeam");

    public static readonly Error UnsupportedStatusChange =
        Error.Validation("Solo se permite cambiar el estado a 'cancelled'. Para finalizar un partido registre su resultado.", "Match.UnsupportedStatusChange");

    public static readonly Error ResultAlreadyRegistered =
        Error.Conflict("El partido ya tiene un resultado registrado.", "Match.ResultAlreadyRegistered");

    public static readonly Error MatchCancelled =
        Error.Conflict("El partido está cancelado.", "Match.Cancelled");

    public static Error NotFound(Guid matchId) =>
        Error.NotFound($"No se encontró el partido con id '{matchId}'.", "Match.NotFound");
}

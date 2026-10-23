using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Domain.Players;

/// <summary>
/// Catálogo de errores del agregado Player.
/// </summary>
public static class PlayerErrors
{
    public static readonly Error TeamRequired =
        Error.Validation("El equipo del jugador es obligatorio.", "Player.TeamRequired");

    public static readonly Error FirstNameTooLong =
        Error.Validation($"El nombre no puede exceder {Player.NameMaxLength} caracteres.", "Player.FirstNameTooLong");

    public static readonly Error LastNameRequired =
        Error.Validation("El apellido del jugador es obligatorio.", "Player.LastNameRequired");

    public static readonly Error LastNameTooLong =
        Error.Validation($"El apellido no puede exceder {Player.NameMaxLength} caracteres.", "Player.LastNameTooLong");

    public static readonly Error JerseyNumberOutOfRange =
        Error.Validation($"El dorsal debe estar entre {Player.MinJerseyNumber} y {Player.MaxJerseyNumber}.", "Player.JerseyNumberOutOfRange");

    public static readonly Error InvalidPosition =
        Error.Validation("La posición del jugador no es válida.", "Player.InvalidPosition");

    public static readonly Error JerseyNumberTaken =
        Error.Conflict("El dorsal ya está asignado a otro jugador del equipo.", "Player.JerseyNumberTaken");

    public static Error NotFound(Guid playerId) =>
        Error.NotFound($"No se encontró el jugador con id '{playerId}'.", "Player.NotFound");
}

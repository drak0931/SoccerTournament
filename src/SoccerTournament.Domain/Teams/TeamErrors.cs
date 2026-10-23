using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Domain.Teams;

/// <summary>
/// Catálogo de errores del agregado Team.
/// </summary>
public static class TeamErrors
{
    public static readonly Error FullNameRequired =
        Error.Validation("El nombre completo del equipo es obligatorio.", "Team.FullNameRequired");

    public static readonly Error FullNameTooLong =
        Error.Validation($"El nombre completo no puede exceder {Team.FullNameMaxLength} caracteres.", "Team.FullNameTooLong");

    public static readonly Error ShortNameRequired =
        Error.Validation("El nombre corto del equipo es obligatorio.", "Team.ShortNameRequired");

    public static readonly Error ShortNameTooLong =
        Error.Validation($"El nombre corto no puede exceder {Team.ShortNameMaxLength} caracteres.", "Team.ShortNameTooLong");

    public static readonly Error FoundedYearOutOfRange =
        Error.Validation($"El año de fundación debe estar entre {Team.MinFoundedYear} y {Team.MaxFoundedYear}.", "Team.FoundedYearOutOfRange");

    public static readonly Error FullNameAlreadyExists =
        Error.Conflict("Ya existe un equipo con ese nombre completo.", "Team.FullNameAlreadyExists");

    public static readonly Error ShortNameAlreadyExists =
        Error.Conflict("Ya existe un equipo con ese nombre corto.", "Team.ShortNameAlreadyExists");

    public static readonly Error HasDependencies =
        Error.Conflict("No se puede eliminar el equipo porque tiene jugadores o partidos asociados.", "Team.HasDependencies");

    public static Error NotFound(Guid teamId) =>
        Error.NotFound($"No se encontró el equipo con id '{teamId}'.", "Team.NotFound");
}

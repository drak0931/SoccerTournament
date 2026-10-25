using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Infrastructure.Persistence;

/// <summary>
/// Traduce errores de SQL Server a errores de negocio. Las validaciones de Application cubren el caso
/// normal; esto cubre las condiciones de carrera (dos requests simultáneos que pasan la validación).
/// </summary>
internal static class SqlErrorTranslator
{
    // Números de error de SQL Server
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;
    private const int ConstraintConflict = 547;

    // Constraint de BD → error de dominio específico
    private static readonly IReadOnlyDictionary<string, Error> KnownUniqueConstraints = new Dictionary<string, Error>(StringComparer.OrdinalIgnoreCase)
    {
        ["uq_teams_full_name"] = TeamErrors.FullNameAlreadyExists,
        ["uq_teams_short_name"] = TeamErrors.ShortNameAlreadyExists,
        ["uq_players_team_jersey"] = PlayerErrors.JerseyNumberTaken
    };

    public static readonly Error DuplicateRecord =
        Error.Conflict("El registro ya existe.", "Database.DuplicateRecord");

    public static readonly Error ConstraintViolation =
        Error.Conflict("La operación entra en conflicto con datos relacionados.", "Database.ConstraintViolation");

    public static readonly Error ConcurrencyConflict =
        Error.Conflict("El registro fue modificado por otro proceso. Consulte la versión actual y vuelva a intentarlo.", "Database.ConcurrencyConflict");

    /// <summary>
    /// Devuelve el error de negocio correspondiente, o null si el error SQL no es un conflicto conocido.
    /// </summary>
    public static Error? Translate(int sqlErrorNumber, string sqlErrorMessage)
    {
        switch (sqlErrorNumber)
        {
            case UniqueConstraintViolation:
            case UniqueIndexViolation:
                var knownConstraint = KnownUniqueConstraints.Keys
                    .FirstOrDefault(constraint => sqlErrorMessage.Contains(constraint, StringComparison.OrdinalIgnoreCase));
                return knownConstraint is null ? DuplicateRecord : KnownUniqueConstraints[knownConstraint];

            case ConstraintConflict:
                return ConstraintViolation;

            default:
                return null;
        }
    }
}

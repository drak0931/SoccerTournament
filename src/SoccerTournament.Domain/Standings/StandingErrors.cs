using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Domain.Standings;

/// <summary>
/// Catálogo de errores de la tabla de posiciones.
/// </summary>
public static class StandingErrors
{
    public static readonly Error NegativeGoals =
        Error.Validation("Los goles no pueden ser negativos.", "Standing.NegativeGoals");

    public static Error NotFound(Guid teamId) =>
        Error.NotFound($"No existe la fila de posiciones para el equipo '{teamId}'.", "Standing.NotFound");
}

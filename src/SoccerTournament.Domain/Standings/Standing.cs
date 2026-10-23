using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Domain.Standings;

/// <summary>
/// Fila de la tabla de posiciones de un equipo (tabla materializada dbo.standings).
/// Su Id es el Id del equipo. Se actualiza en la misma transacción que registra el resultado.
/// </summary>
public sealed class Standing : AggregateRoot
{
    public const int PointsPerWin = 3;
    public const int PointsPerDraw = 1;

    private Standing(Guid teamId)
        : base(teamId)
    {
    }

    // Requerido por EF Core
    private Standing()
    {
    }

    public Guid TeamId => Id;

    public int Played { get; private set; }

    public int Won { get; private set; }

    public int Drawn { get; private set; }

    public int Lost { get; private set; }

    public int GoalsFor { get; private set; }

    public int GoalsAgainst { get; private set; }

    public int GoalDifference { get; private set; }

    public int Points { get; private set; }

    /// <summary>
    /// Crea la fila inicial (todo en cero) para un equipo nuevo.
    /// </summary>
    public static Standing CreateFor(Guid teamId) => new(teamId);

    /// <summary>
    /// Acumula el resultado de un partido desde la perspectiva de este equipo.
    /// </summary>
    public Result ApplyResult(int goalsFor, int goalsAgainst)
    {
        if (goalsFor < 0 || goalsAgainst < 0)
        {
            return StandingErrors.NegativeGoals;
        }

        Played++;
        GoalsFor += goalsFor;
        GoalsAgainst += goalsAgainst;
        GoalDifference = GoalsFor - GoalsAgainst;

        if (goalsFor > goalsAgainst)
        {
            Won++;
            Points += PointsPerWin;
        }
        else if (goalsFor == goalsAgainst)
        {
            Drawn++;
            Points += PointsPerDraw;
        }
        else
        {
            Lost++;
        }

        return Result.Success();
    }
}

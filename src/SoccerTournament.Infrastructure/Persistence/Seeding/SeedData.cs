using SoccerTournament.Domain.Players;

namespace SoccerTournament.Infrastructure.Persistence.Seeding;

/// <summary>
/// Datos ficticios de prueba: 4 equipos, 5 jugadores por equipo, 6 partidos (todos contra todos) y 3 resultados.
/// Las fechas son relativas al día del seed para que siempre haya partidos jugados, pendientes y futuros.
/// </summary>
internal static class SeedData
{
    public sealed record TeamSeed(string FullName, string ShortName, DateTime FoundedYear, IReadOnlyList<PlayerSeed> Players);

    public sealed record PlayerSeed(string FirstName, string LastName, int JerseyNumber, PlayerPosition Position);

    public sealed record GoalSeed(string TeamShortName, int JerseyNumber, int Minute);

    public sealed record ResultSeed(int HomeGoals, int AwayGoals, IReadOnlyList<GoalSeed> Goals);

    /// <param name="DaysFromSeedDate">Negativo: partido pasado. Positivo: partido futuro.</param>
    public sealed record MatchSeed(string HomeShortName, string AwayShortName, int DaysFromSeedDate, ResultSeed? Result);

    /// <summary>
    /// Hora de los partidos: 01:00 UTC = 19:00 hora de El Salvador (UTC-6).
    /// </summary>
    public static readonly TimeSpan KickOffTimeUtc = TimeSpan.FromHours(1);

    public static readonly IReadOnlyList<TeamSeed> Teams = new[]
    {
        new TeamSeed("Club Deportivo Volcanes", "VOL", new DateTime(1925, 3, 15, 0, 0, 0, DateTimeKind.Utc), new[]
        {
            new PlayerSeed("Mario", "Ventura", 1, PlayerPosition.Goalkeeper),
            new PlayerSeed("Luis", "Cañas", 4, PlayerPosition.Defender),
            new PlayerSeed("Óscar", "Mejía", 8, PlayerPosition.Midfielder),
            new PlayerSeed("Kevin", "Alfaro", 9, PlayerPosition.Forward),
            new PlayerSeed("Erick", "Portillo", 10, PlayerPosition.Forward)
        }),
        new TeamSeed("Atlético Costa del Sol", "ACS", new DateTime(1948, 7, 2, 0, 0, 0, DateTimeKind.Utc), new[]
        {
            new PlayerSeed("Daniel", "Guevara", 1, PlayerPosition.Goalkeeper),
            new PlayerSeed("Carlos", "Orellana", 4, PlayerPosition.Defender),
            new PlayerSeed("Jaime", "Rivas", 8, PlayerPosition.Midfielder),
            new PlayerSeed("Rodrigo", "Pineda", 9, PlayerPosition.Forward),
            new PlayerSeed("Samuel", "Henríquez", 10, PlayerPosition.Forward)
        }),
        new TeamSeed("Real Montecristo FC", "RMC", new DateTime(1960, 11, 20, 0, 0, 0, DateTimeKind.Utc), new[]
        {
            new PlayerSeed("Andrés", "Molina", 1, PlayerPosition.Goalkeeper),
            new PlayerSeed("Felipe", "Zelaya", 4, PlayerPosition.Defender),
            new PlayerSeed("Hugo", "Escobar", 8, PlayerPosition.Midfielder),
            new PlayerSeed("Diego", "Ramírez", 9, PlayerPosition.Forward),
            new PlayerSeed("Pablo", "Quintanilla", 10, PlayerPosition.Forward)
        }),
        new TeamSeed("Deportivo Los Pinos", "DLP", new DateTime(1975, 1, 9, 0, 0, 0, DateTimeKind.Utc), new[]
        {
            new PlayerSeed("Ernesto", "Bonilla", 1, PlayerPosition.Goalkeeper),
            new PlayerSeed("Raúl", "Flores", 4, PlayerPosition.Defender),
            new PlayerSeed("Tomás", "Argueta", 8, PlayerPosition.Midfielder),
            new PlayerSeed("Iván", "Castro", 9, PlayerPosition.Forward),
            new PlayerSeed("Marcos", "Velásquez", 10, PlayerPosition.Forward)
        })
    };

    public static readonly IReadOnlyList<MatchSeed> Matches = new[]
    {
        // Jornada 1 (jugada)
        new MatchSeed("VOL", "ACS", -14, new ResultSeed(2, 1, new[]
        {
            new GoalSeed("VOL", 9, 12),
            new GoalSeed("VOL", 10, 67),
            new GoalSeed("ACS", 9, 80)
        })),
        new MatchSeed("RMC", "DLP", -14, new ResultSeed(0, 0, Array.Empty<GoalSeed>())),

        // Jornada 2 (un partido jugado y otro pendiente de registrar resultado)
        new MatchSeed("VOL", "RMC", -7, new ResultSeed(1, 3, new[]
        {
            new GoalSeed("RMC", 9, 15),
            new GoalSeed("VOL", 9, 30),
            new GoalSeed("RMC", 10, 55),
            new GoalSeed("RMC", 9, 88)
        })),
        new MatchSeed("ACS", "DLP", -7, null),

        // Jornada 3 (futura)
        new MatchSeed("VOL", "DLP", 7, null),
        new MatchSeed("ACS", "RMC", 7, null)
    };
}

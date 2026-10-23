using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Domain.Matches;

/// <summary>
/// Gol anotado en un partido. Pertenece al agregado Match: solo se crea al registrar el resultado.
/// </summary>
public sealed class Goal : Entity
{
    public const int MinMinute = 1;
    public const int MaxMinute = 130;

    private Goal(Guid id, Guid matchId, Guid playerId, Guid teamId, byte minute)
        : base(id)
    {
        MatchId = matchId;
        PlayerId = playerId;
        TeamId = teamId;
        Minute = minute;
    }

    // Requerido por EF Core
    private Goal()
    {
    }

    public Guid MatchId { get; private set; }

    public Guid PlayerId { get; private set; }

    public Guid TeamId { get; private set; }

    public byte Minute { get; private set; }

    internal static Goal Create(Guid matchId, ScoredGoal scoredGoal) =>
        new(Guid.NewGuid(), matchId, scoredGoal.PlayerId, scoredGoal.TeamId, (byte)scoredGoal.Minute);
}

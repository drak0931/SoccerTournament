namespace SoccerTournament.Domain.Matches;

/// <summary>
/// Dato de entrada para registrar un gol dentro del resultado de un partido.
/// </summary>
public sealed record ScoredGoal(Guid PlayerId, Guid TeamId, int Minute);

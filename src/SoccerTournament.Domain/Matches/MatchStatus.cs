namespace SoccerTournament.Domain.Matches;

/// <summary>
/// Estado del partido. Se persiste como texto ('scheduled', 'finished', 'cancelled') según ck_matches_status.
/// </summary>
public enum MatchStatus
{
    Scheduled = 1,
    Finished = 2,
    Cancelled = 3
}

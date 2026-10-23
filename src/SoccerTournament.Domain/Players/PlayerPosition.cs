namespace SoccerTournament.Domain.Players;

/// <summary>
/// Posición del jugador. Se persiste como texto ('Goalkeeper', 'Defender', ...) según ck_players_position.
/// </summary>
public enum PlayerPosition
{
    Goalkeeper = 1,
    Defender = 2,
    Midfielder = 3,
    Forward = 4
}

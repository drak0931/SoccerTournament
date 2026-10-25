using SoccerTournament.Domain.Matches;
using SoccerTournament.Domain.Players;
using SoccerTournament.Domain.Standings;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Abstractions.Persistence.Write;

// Repositorios del lado de escritura (EF Core). Cargan agregados para modificarlos y registran cambios,
// pero nunca guardan: la confirmación es responsabilidad exclusiva de IUnitOfWork.

public interface ITeamRepository
{
    Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(Guid teamId, CancellationToken cancellationToken);

    /// <summary>
    /// Indica si hay al menos un equipo registrado (usado por el seed para detectar una BD vacía).
    /// </summary>
    Task<bool> AnyAsync(CancellationToken cancellationToken);

    Task<bool> FullNameExistsAsync(string fullName, Guid? excludeTeamId, CancellationToken cancellationToken);

    Task<bool> ShortNameExistsAsync(string shortName, Guid? excludeTeamId, CancellationToken cancellationToken);

    /// <summary>
    /// Indica si el equipo tiene jugadores o partidos asociados (impide eliminarlo).
    /// </summary>
    Task<bool> HasDependenciesAsync(Guid teamId, CancellationToken cancellationToken);

    void Add(Team team);

    void Remove(Team team);
}

public interface IPlayerRepository
{
    Task<bool> JerseyNumberExistsAsync(Guid teamId, int jerseyNumber, CancellationToken cancellationToken);

    /// <summary>
    /// Devuelve el equipo de cada jugador solicitado (los inexistentes no aparecen en el diccionario).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, Guid>> GetTeamIdsAsync(IReadOnlyCollection<Guid> playerIds, CancellationToken cancellationToken);

    void Add(Player player);
}

public interface IMatchRepository
{
    /// <summary>
    /// Carga el partido con sus goles (agregado completo).
    /// </summary>
    Task<Match?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken);

    void Add(Match match);
}

public interface IStandingRepository
{
    Task<IReadOnlyList<Standing>> GetByTeamIdsAsync(IReadOnlyCollection<Guid> teamIds, CancellationToken cancellationToken);

    void Add(Standing standing);
}

using Microsoft.EntityFrameworkCore;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Players;

namespace SoccerTournament.Infrastructure.Persistence.Repositories;

internal sealed class PlayerRepository : IPlayerRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PlayerRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> JerseyNumberExistsAsync(Guid teamId, int jerseyNumber, CancellationToken cancellationToken) =>
        _dbContext.Players.AnyAsync(
            player => player.TeamId == teamId && player.JerseyNumber == jerseyNumber,
            cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, Guid>> GetTeamIdsAsync(IReadOnlyCollection<Guid> playerIds, CancellationToken cancellationToken) =>
        await _dbContext.Players
            .AsNoTracking()
            .Where(player => playerIds.Contains(player.Id))
            .Select(player => new { player.Id, player.TeamId })
            .ToDictionaryAsync(player => player.Id, player => player.TeamId, cancellationToken);

    public void Add(Player player) => _dbContext.Players.Add(player);
}

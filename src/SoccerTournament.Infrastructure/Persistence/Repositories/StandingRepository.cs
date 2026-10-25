using Microsoft.EntityFrameworkCore;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Standings;

namespace SoccerTournament.Infrastructure.Persistence.Repositories;

internal sealed class StandingRepository : IStandingRepository
{
    private readonly ApplicationDbContext _dbContext;

    public StandingRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Standing>> GetByTeamIdsAsync(IReadOnlyCollection<Guid> teamIds, CancellationToken cancellationToken) =>
        await _dbContext.Standings
            .Where(standing => teamIds.Contains(standing.Id))
            .ToListAsync(cancellationToken);

    public void Add(Standing standing) => _dbContext.Standings.Add(standing);
}

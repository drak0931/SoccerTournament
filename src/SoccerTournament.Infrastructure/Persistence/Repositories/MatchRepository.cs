using Microsoft.EntityFrameworkCore;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Matches;

namespace SoccerTournament.Infrastructure.Persistence.Repositories;

internal sealed class MatchRepository : IMatchRepository
{
    private readonly ApplicationDbContext _dbContext;

    public MatchRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // Carga el agregado completo (partido + goles) para mantener sus invariantes
    public Task<Match?> GetByIdAsync(Guid matchId, CancellationToken cancellationToken) =>
        _dbContext.Matches
            .Include(match => match.Goals)
            .FirstOrDefaultAsync(match => match.Id == matchId, cancellationToken);

    public void Add(Match match) => _dbContext.Matches.Add(match);
}

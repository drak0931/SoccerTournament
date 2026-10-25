using Microsoft.EntityFrameworkCore;
using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repositorio de escritura de equipos. Las verificaciones (Exists/Any) son reglas del lado de escritura,
/// no consultas de lectura para el cliente (esas se resuelven con Dapper).
/// </summary>
internal sealed class TeamRepository : ITeamRepository
{
    private readonly ApplicationDbContext _dbContext;

    public TeamRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Team?> GetByIdAsync(Guid teamId, CancellationToken cancellationToken) =>
        _dbContext.Teams.FirstOrDefaultAsync(team => team.Id == teamId, cancellationToken);

    public Task<bool> ExistsAsync(Guid teamId, CancellationToken cancellationToken) =>
        _dbContext.Teams.AnyAsync(team => team.Id == teamId, cancellationToken);

    public Task<bool> FullNameExistsAsync(string fullName, Guid? excludeTeamId, CancellationToken cancellationToken) =>
        _dbContext.Teams.AnyAsync(
            team => team.FullName == fullName && (excludeTeamId == null || team.Id != excludeTeamId),
            cancellationToken);

    public Task<bool> ShortNameExistsAsync(string shortName, Guid? excludeTeamId, CancellationToken cancellationToken) =>
        _dbContext.Teams.AnyAsync(
            team => team.ShortName == shortName && (excludeTeamId == null || team.Id != excludeTeamId),
            cancellationToken);

    public async Task<bool> HasDependenciesAsync(Guid teamId, CancellationToken cancellationToken) =>
        await _dbContext.Players.AnyAsync(player => player.TeamId == teamId, cancellationToken)
        || await _dbContext.Matches.AnyAsync(match => match.HomeTeamId == teamId || match.AwayTeamId == teamId, cancellationToken);

    public void Add(Team team) => _dbContext.Teams.Add(team);

    public void Remove(Team team) => _dbContext.Teams.Remove(team);
}

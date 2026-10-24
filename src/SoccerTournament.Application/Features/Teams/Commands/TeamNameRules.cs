using SoccerTournament.Application.Abstractions.Persistence.Write;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Teams;

namespace SoccerTournament.Application.Features.Teams.Commands;

/// <summary>
/// Regla de unicidad de nombres de equipo (requiere consultar la BD, por eso no vive en Domain).
/// La BD la respalda con uq_teams_full_name y uq_teams_short_name ante condiciones de carrera.
/// </summary>
internal static class TeamNameRules
{
    public static async Task<Result> EnsureUniqueAsync(
        ITeamRepository teamRepository,
        string fullName,
        string shortName,
        Guid? excludeTeamId,
        CancellationToken cancellationToken)
    {
        if (await teamRepository.FullNameExistsAsync(fullName.Trim(), excludeTeamId, cancellationToken))
        {
            return TeamErrors.FullNameAlreadyExists;
        }

        if (await teamRepository.ShortNameExistsAsync(shortName.Trim(), excludeTeamId, cancellationToken))
        {
            return TeamErrors.ShortNameAlreadyExists;
        }

        return Result.Success();
    }
}

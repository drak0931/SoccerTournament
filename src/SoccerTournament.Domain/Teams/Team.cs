using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Teams.Events;

namespace SoccerTournament.Domain.Teams;

/// <summary>
/// Equipo participante del torneo.
/// </summary>
public sealed class Team : AggregateRoot
{
    public const int FullNameMaxLength = 100;
    public const int ShortNameMaxLength = 50;
    public const int MinFoundedYear = 1850;
    public const int MaxFoundedYear = 2100;

    private Team(Guid id, string fullName, string shortName, DateTime? foundedYear)
        : base(id)
    {
        FullName = fullName;
        ShortName = shortName;
        FoundedYear = foundedYear;
    }

    // Requerido por EF Core
    private Team()
    {
    }

    public string FullName { get; private set; } = null!;

    public string ShortName { get; private set; } = null!;

    /// <summary>
    /// Fecha de fundación (columna founded_year de tipo DATETIME).
    /// </summary>
    public DateTime? FoundedYear { get; private set; }

    public static Result<Team> Create(string fullName, string shortName, DateTime? foundedYear)
    {
        var validation = Validate(fullName, shortName, foundedYear);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        var team = new Team(Guid.NewGuid(), fullName.Trim(), shortName.Trim(), foundedYear);

        team.Raise(new TeamCreatedDomainEvent(team.Id, team.FullName, team.ShortName));

        return team;
    }

    /// <summary>
    /// Reemplaza todos los datos editables del equipo (semántica de PUT; PATCH combina antes de invocarlo).
    /// </summary>
    public Result Update(string fullName, string shortName, DateTime? foundedYear)
    {
        var validation = Validate(fullName, shortName, foundedYear);
        if (validation.IsFailure)
        {
            return validation;
        }

        FullName = fullName.Trim();
        ShortName = shortName.Trim();
        FoundedYear = foundedYear;

        Raise(new TeamUpdatedDomainEvent(Id));

        return Result.Success();
    }

    /// <summary>
    /// Marca el equipo para eliminación. Las dependencias (jugadores, partidos) se validan en Application.
    /// </summary>
    public void MarkAsDeleted() => Raise(new TeamDeletedDomainEvent(Id));

    private static Result Validate(string fullName, string shortName, DateTime? foundedYear)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            errors.Add(TeamErrors.FullNameRequired);
        }
        else if (fullName.Trim().Length > FullNameMaxLength)
        {
            errors.Add(TeamErrors.FullNameTooLong);
        }

        if (string.IsNullOrWhiteSpace(shortName))
        {
            errors.Add(TeamErrors.ShortNameRequired);
        }
        else if (shortName.Trim().Length > ShortNameMaxLength)
        {
            errors.Add(TeamErrors.ShortNameTooLong);
        }

        if (foundedYear is { Year: < MinFoundedYear or > MaxFoundedYear })
        {
            errors.Add(TeamErrors.FoundedYearOutOfRange);
        }

        return errors.Count switch
        {
            0 => Result.Success(),
            1 => errors[0],
            _ => new ValidationError(errors)
        };
    }
}

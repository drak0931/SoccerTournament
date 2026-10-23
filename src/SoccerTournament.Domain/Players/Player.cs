using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Domain.Players.Events;

namespace SoccerTournament.Domain.Players;

/// <summary>
/// Integrante de un equipo. Es un agregado independiente que referencia al equipo por Id,
/// para no cargar la plantilla completa al modificar un equipo.
/// </summary>
public sealed class Player : AggregateRoot
{
    public const int NameMaxLength = 60;
    public const int MinJerseyNumber = 1;
    public const int MaxJerseyNumber = 99;

    private Player(Guid id, Guid teamId, string? firstName, string lastName, byte jerseyNumber, PlayerPosition position)
        : base(id)
    {
        TeamId = teamId;
        FirstName = firstName;
        LastName = lastName;
        JerseyNumber = jerseyNumber;
        Position = position;
    }

    // Requerido por EF Core
    private Player()
    {
    }

    public Guid TeamId { get; private set; }

    public string? FirstName { get; private set; }

    public string LastName { get; private set; } = null!;

    public byte JerseyNumber { get; private set; }

    public PlayerPosition Position { get; private set; }

    /// <summary>
    /// Registra un jugador en un equipo. La unicidad del dorsal por equipo se valida en Application.
    /// </summary>
    public static Result<Player> Register(Guid teamId, string? firstName, string lastName, int jerseyNumber, PlayerPosition position)
    {
        var errors = new List<Error>();

        if (teamId == Guid.Empty)
        {
            errors.Add(PlayerErrors.TeamRequired);
        }

        if (firstName is not null && firstName.Trim().Length > NameMaxLength)
        {
            errors.Add(PlayerErrors.FirstNameTooLong);
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            errors.Add(PlayerErrors.LastNameRequired);
        }
        else if (lastName.Trim().Length > NameMaxLength)
        {
            errors.Add(PlayerErrors.LastNameTooLong);
        }

        if (jerseyNumber is < MinJerseyNumber or > MaxJerseyNumber)
        {
            errors.Add(PlayerErrors.JerseyNumberOutOfRange);
        }

        if (!Enum.IsDefined(position))
        {
            errors.Add(PlayerErrors.InvalidPosition);
        }

        if (errors.Count > 0)
        {
            return errors.Count == 1 ? errors[0] : new ValidationError(errors);
        }

        // Un nombre vacío se normaliza a null (columna opcional)
        var normalizedFirstName = string.IsNullOrWhiteSpace(firstName) ? null : firstName.Trim();

        var player = new Player(Guid.NewGuid(), teamId, normalizedFirstName, lastName.Trim(), (byte)jerseyNumber, position);

        player.Raise(new PlayerRegisteredDomainEvent(player.Id, player.TeamId, player.JerseyNumber));

        return player;
    }
}

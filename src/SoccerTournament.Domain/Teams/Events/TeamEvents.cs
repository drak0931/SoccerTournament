using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Domain.Teams.Events;

/// <summary>
/// Se emite al registrar un nuevo equipo.
/// </summary>
public sealed record TeamCreatedDomainEvent(Guid TeamId, string FullName, string ShortName) : DomainEvent;

/// <summary>
/// Se emite al modificar los datos de un equipo (PUT o PATCH).
/// </summary>
public sealed record TeamUpdatedDomainEvent(Guid TeamId) : DomainEvent;

/// <summary>
/// Se emite al eliminar un equipo.
/// </summary>
public sealed record TeamDeletedDomainEvent(Guid TeamId) : DomainEvent;

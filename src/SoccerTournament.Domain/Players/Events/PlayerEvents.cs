using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Domain.Players.Events;

/// <summary>
/// Se emite al registrar un jugador en un equipo. No incluye nombres para minimizar datos personales en logs.
/// </summary>
public sealed record PlayerRegisteredDomainEvent(Guid PlayerId, Guid TeamId, byte JerseyNumber) : DomainEvent;

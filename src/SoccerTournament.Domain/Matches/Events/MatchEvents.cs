using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Domain.Matches.Events;

/// <summary>
/// Se emite al crear (programar) un partido.
/// </summary>
public sealed record MatchScheduledDomainEvent(Guid MatchId, Guid HomeTeamId, Guid AwayTeamId, DateTime ScheduledAt) : DomainEvent;

/// <summary>
/// Se emite al registrar el resultado de un partido. Dispara la actualización de la tabla de posiciones.
/// </summary>
public sealed record MatchResultRegisteredDomainEvent(Guid MatchId, Guid HomeTeamId, Guid AwayTeamId, int HomeGoals, int AwayGoals) : DomainEvent;

/// <summary>
/// Se emite al cancelar un partido programado.
/// </summary>
public sealed record MatchCancelledDomainEvent(Guid MatchId) : DomainEvent;

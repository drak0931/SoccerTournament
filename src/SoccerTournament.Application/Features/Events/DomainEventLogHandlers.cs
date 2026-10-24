using Microsoft.Extensions.Logging;
using SoccerTournament.Application.Abstractions.Events;
using SoccerTournament.Domain.Matches.Events;
using SoccerTournament.Domain.Players.Events;
using SoccerTournament.Domain.Teams.Events;

namespace SoccerTournament.Application.Features.Events;

// Handlers que registran en log estructurado los eventos de dominio. Se ejecutan dentro del
// scope del request, por lo que heredan el TraceId/CorrelationId y quedan enlazados al flujo.

internal sealed class TeamCreatedDomainEventHandler : IDomainEventHandler<TeamCreatedDomainEvent>
{
    private readonly ILogger<TeamCreatedDomainEventHandler> _logger;

    public TeamCreatedDomainEventHandler(ILogger<TeamCreatedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(TeamCreatedDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Evento de dominio {EventName} {EventId}: equipo {TeamId} ({ShortName}) creado",
            nameof(TeamCreatedDomainEvent), domainEvent.EventId, domainEvent.TeamId, domainEvent.ShortName);

        return Task.CompletedTask;
    }
}

internal sealed class PlayerRegisteredDomainEventHandler : IDomainEventHandler<PlayerRegisteredDomainEvent>
{
    private readonly ILogger<PlayerRegisteredDomainEventHandler> _logger;

    public PlayerRegisteredDomainEventHandler(ILogger<PlayerRegisteredDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(PlayerRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        // Sin nombres del jugador: solo identificadores
        _logger.LogInformation(
            "Evento de dominio {EventName} {EventId}: jugador {PlayerId} registrado en equipo {TeamId} con dorsal {JerseyNumber}",
            nameof(PlayerRegisteredDomainEvent), domainEvent.EventId, domainEvent.PlayerId, domainEvent.TeamId, domainEvent.JerseyNumber);

        return Task.CompletedTask;
    }
}

internal sealed class MatchScheduledDomainEventHandler : IDomainEventHandler<MatchScheduledDomainEvent>
{
    private readonly ILogger<MatchScheduledDomainEventHandler> _logger;

    public MatchScheduledDomainEventHandler(ILogger<MatchScheduledDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(MatchScheduledDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Evento de dominio {EventName} {EventId}: partido {MatchId} programado {HomeTeamId} vs {AwayTeamId} para {ScheduledAt}",
            nameof(MatchScheduledDomainEvent), domainEvent.EventId, domainEvent.MatchId,
            domainEvent.HomeTeamId, domainEvent.AwayTeamId, domainEvent.ScheduledAt);

        return Task.CompletedTask;
    }
}

internal sealed class MatchResultRegisteredDomainEventHandler : IDomainEventHandler<MatchResultRegisteredDomainEvent>
{
    private readonly ILogger<MatchResultRegisteredDomainEventHandler> _logger;

    public MatchResultRegisteredDomainEventHandler(ILogger<MatchResultRegisteredDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(MatchResultRegisteredDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Evento de dominio {EventName} {EventId}: resultado registrado en partido {MatchId} ({HomeTeamId} {HomeGoals} - {AwayGoals} {AwayTeamId})",
            nameof(MatchResultRegisteredDomainEvent), domainEvent.EventId, domainEvent.MatchId,
            domainEvent.HomeTeamId, domainEvent.HomeGoals, domainEvent.AwayGoals, domainEvent.AwayTeamId);

        return Task.CompletedTask;
    }
}

using SoccerTournament.Domain.Abstractions;

namespace SoccerTournament.Application.Abstractions.Events;

/// <summary>
/// Reacciona a un evento de dominio. Infrastructure invoca los handlers después de confirmar
/// la transacción, cuando el evento ya quedó registrado en dbo.domain_events.
/// </summary>
public interface IDomainEventHandler<in TDomainEvent>
    where TDomainEvent : IDomainEvent
{
    Task HandleAsync(TDomainEvent domainEvent, CancellationToken cancellationToken);
}

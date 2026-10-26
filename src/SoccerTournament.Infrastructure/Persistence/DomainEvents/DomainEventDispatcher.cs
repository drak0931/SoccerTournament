using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoccerTournament.Application.Abstractions.Events;
using SoccerTournament.Domain.Abstractions;
using SoccerTournament.Infrastructure.Diagnostics;

namespace SoccerTournament.Infrastructure.Persistence.DomainEvents;

/// <summary>
/// Entrega los eventos de dominio a sus handlers en proceso, después de confirmar la transacción.
/// Un fallo en un handler se registra pero no revierte la operación ya confirmada.
/// </summary>
internal sealed class DomainEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, (Type HandlerType, MethodInfo HandleMethod)> HandlerCache = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        foreach (var domainEvent in domainEvents)
        {
            using var activity = InfrastructureDiagnostics.ActivitySource.StartActivity($"DomainEvent {domainEvent.GetType().Name}");
            activity?.SetTag("app.domain_event.id", domainEvent.EventId);

            var (handlerType, handleMethod) = HandlerCache.GetOrAdd(domainEvent.GetType(), static eventType =>
            {
                var type = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
                return (type, type.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!);
            });

            foreach (var handler in _serviceProvider.GetServices(handlerType))
            {
                try
                {
                    await (Task)handleMethod.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
                }
                catch (Exception exception)
                {
                    // Límite de infraestructura: el cambio ya está confirmado y el evento persistido en domain_events
                    _logger.LogError(
                        exception,
                        "Error en handler {HandlerType} al procesar el evento {EventType} {EventId}",
                        handler!.GetType().Name, domainEvent.GetType().Name, domainEvent.EventId);
                }
            }
        }
    }
}

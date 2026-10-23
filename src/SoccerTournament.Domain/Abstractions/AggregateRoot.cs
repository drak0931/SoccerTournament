namespace SoccerTournament.Domain.Abstractions;

/// <summary>
/// Raíz de agregado: frontera de consistencia y único punto que acumula eventos de dominio.
/// El UnitOfWork recoge los eventos al confirmar la transacción.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}

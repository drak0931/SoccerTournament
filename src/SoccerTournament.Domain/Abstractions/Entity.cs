namespace SoccerTournament.Domain.Abstractions;

/// <summary>
/// Entidad con identidad propia. Dos entidades son iguales si tienen el mismo tipo e Id.
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
    protected Entity(Guid id)
    {
        Id = id;
    }

    // Requerido por EF Core para materializar la entidad
    protected Entity()
    {
    }

    public Guid Id { get; protected init; }

    public bool Equals(Entity? other) =>
        other is not null && other.GetType() == GetType() && other.Id == Id;

    public override bool Equals(object? obj) => obj is Entity entity && Equals(entity);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right) => Equals(left, right);

    public static bool operator !=(Entity? left, Entity? right) => !Equals(left, right);
}

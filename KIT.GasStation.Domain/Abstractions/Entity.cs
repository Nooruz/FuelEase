namespace KIT.GasStation.Domain.Abstractions;

/// <summary>
/// Базовый класс сущности с идентификатором.
/// Обеспечивает identity-based equality по Id.
/// </summary>
public abstract class Entity
{
    /// <summary>
    /// Первичный ключ сущности.
    /// </summary>
    public int Id { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not Entity other || other.GetType() != GetType())
            return false;

        // Transient entities (Id == 0) are only equal to themselves
        if (Id == 0 || other.Id == 0)
            return ReferenceEquals(this, other);

        return Id == other.Id;
    }

    public override int GetHashCode() => Id == 0
        ? base.GetHashCode()
        : HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entity? left, Entity? right)
        => left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity? left, Entity? right)
        => !(left == right);
}

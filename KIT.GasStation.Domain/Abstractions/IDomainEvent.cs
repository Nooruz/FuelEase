namespace KIT.GasStation.Domain.Abstractions;

/// <summary>
/// Маркер доменного события. Каждое событие описывает что-то значимое, что произошло в домене.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Момент возникновения события.
    /// </summary>
    DateTime OccurredAt { get; }
}

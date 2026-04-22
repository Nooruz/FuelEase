namespace KIT.GasStation.Domain.Abstractions;

/// <summary>
/// Базовый класс агрегатного корня.
/// Агрегатный корень управляет доменными событиями и является точкой входа для операций над агрегатом.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    /// <summary>
    /// Доменные события, сгенерированные агрегатом (для публикации после сохранения).
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Добавить доменное событие.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
        => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Очистить накопленные доменные события (вызывается после публикации).
    /// </summary>
    public void ClearDomainEvents()
        => _domainEvents.Clear();
}

using KIT.GasStation.Domain.Abstractions;

namespace KIT.GasStation.Domain.Models;

/// <summary>
/// Устаревший базовый класс. Сохранён для обратной совместимости с <see cref="NonQueryDataService{T}"/>.
/// Новые сущности должны наследовать <see cref="Entity"/> или <see cref="AggregateRoot"/> напрямую.
/// </summary>
public abstract class DomainObject : Entity
{
    // INotifyPropertyChanged удалён — доменные объекты не должны зависеть от WPF/UI инфраструктуры.
    // Update(DomainObject) удалён — обновляйте свойства напрямую через сервисный или прикладной уровень.
}

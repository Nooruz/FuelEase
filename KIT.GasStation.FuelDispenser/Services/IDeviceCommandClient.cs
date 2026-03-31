using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.FuelDispenser.Services
{
    /// <summary>
    /// Команды, которые сервер отправляет воркеру/оборудованию.
    /// </summary>
    public interface IDeviceCommandClient
    {
        /// <summary>
        /// Установить цены сразу по нескольким группам.
        /// </summary>
        Task SetPricesAsync(Guid commandId, IReadOnlyCollection<PriceRequest> prices);

        /// <summary>
        /// Установить цену по одной группе.
        /// </summary>
        Task SetPriceAsync(Guid commandId, PriceRequest priceRequest);

        /// <summary>
        /// Начать заправку.
        /// </summary>
        Task StartFuelingAsync(FuelingRequest fuelingRequest);

        /// <summary>
        /// Остановить заправку.
        /// </summary>
        Task StopFuelingAsync(string groupName);

        /// <summary>
        /// Продолжить заправку.
        /// </summary>
        Task ResumeFuelingAsync(ResumeFuelingRequest resumeFuelingRequest);

        /// <summary>
        /// Получить текущий статус колонки/ТРК.
        /// </summary>
        Task GetStatusByAddressAsync(string groupName);

        /// <summary>
        /// Завершить заправку.
        /// </summary>
        Task CompleteFuelingAsync(string groupName);

        /// <summary>
        /// Получить счетчик по одной группе.
        /// </summary>
        Task GetCounterAsync(Guid commandId, string groupName);

        /// <summary>
        /// Получить счетчики по всей колонке/группе.
        /// </summary>
        Task GetCountersAsync(Guid commandId, string groupName);

        /// <summary>
        /// Переключить режим управления.
        /// </summary>
        Task ChangeControlModeAsync(Guid commandId, string groupName, bool isProgramMode);

        /// <summary>
        /// Инициализировать конфигурацию оборудования.
        /// </summary>
        Task InitializeConfigurationAsync(Guid commandId, string groupName);
    }
}

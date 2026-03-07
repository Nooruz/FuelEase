using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.Web.Hubs
{
    public interface IDeviceResponseClient
    {
        // Сильная типизация на сервере (по желанию)
        Task StatusChanged(StatusResponse status);
        
        /// <summary>
        /// Сообщает об изменении статуса подключения воркера для указанной группы.
        /// </summary>
        /// <param name="groupName">Группа, соответствующая колонке.</param>
        /// <param name="isOnline">True, если воркер подключен.</param>
        Task WorkerStateChanged(WorkerStateNotification notification);
        Task StartPolling(StartPollingCommand command);
        Task StopPolling(StopPollingCommand command);

        /// <summary>
        /// Установить цены на все топлива
        /// </summary>
        Task SetPricesAsync(Guid commandId, Dictionary<string, decimal> prices);

        /// <summary>
        /// Установить цену на одно топливо
        /// </summary>
        Task SetPriceAsync(Guid commandId, string groupName, decimal price);

        /// <summary>
        /// Начать заправку по сумме или литрам
        /// </summary>
        /// <param name="groupName">Название группы</param>
        /// <param name="sum">Сумма</param>
        /// <param name="bySum">Если true то по сумма, иначе по литражу</param>
        Task StartFuelingAsync(string groupName, decimal sum, bool bySum);

        /// <summary>
        /// Остановить заправку
        /// </summary>
        Task StopFuelingAsync(string groupName);

        /// <summary>
        /// Продолжить заправку
        /// </summary>
        Task ResumeFuelingAsync(string groupName, decimal sum);

        /// <summary>
        /// Получить статус по адресу
        /// </summary>
        Task GetStatusByAddressAsync(string groupName);

        /// <summary>
        /// Завершить заправку
        /// </summary>
        /// <param name="groupName">Название группы</param>
        Task CompleteFuelingAsync(string groupName);

        /// <summary>
        /// Получить счетчики ТРК по одному пистолету
        /// </summary>
        /// <param name="groupName">Название группы</param>
        Task GetCounterAsync(Guid commandId, string groupName);

        /// <summary>
        /// Получить счетчики ТРК по всей колонке
        /// </summary>
        Task GetCountersAsync(Guid commandId, string groupName);

        /// <summary>
        /// Поднята или опущена колонка
        /// </summary>
        Task ColumnLiftedChanged(string groupName, bool isLifted);

        /// <summary>
        /// Получены обновленные счетчики ТРК
        /// </summary>
        /// <returns></returns>
        Task OnCountersUpdated(string groupName, List<CounterData> counterDatas);

        /// <summary>
        /// Получен обновленный счетчик ТРК
        /// </summary>
        Task OnCounterUpdated(CounterData counterData);

        Task OnFuelingAsync(FuelingResponse response);

        Task OnCompletedFuelingAsync(string groupName, decimal? quantity = null);

        Task OnWaitingAsync(string groupName);

        Task OnPumpStopAsync(FuelingResponse response);

        /// <summary>
        /// Меняет режим управления колонкой (программный/ручной)
        /// </summary>
        /// <param name="groupName">Название группы</param>
        /// <param name="isProgramMode">Если true программный, иначе ручной</param>
        /// <returns></returns>
        Task ChangeControlModeAsync(Guid commandId, string groupName, bool isProgramMode);

        Task InitializeConfigurationAsync(Guid commandId, string groupName);
    }
}

using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.Web.Hubs
{
    public interface IDeviceResponseClient
    {
        // Сильная типизация на сервере (по желанию)
        Task StatusChanged(ControllerResponse e);
        
        /// <summary>
        /// Сообщает об изменении статуса подключения воркера для указанной группы.
        /// </summary>
        /// <param name="groupName">Группа, соответствующая колонке.</param>
        /// <param name="isOnline">True, если воркер подключен.</param>
        Task WorkerStateChanged(WorkerStateNotification notification);
        Task StartPolling(StartPollingCommand command);
        Task StopPolling(StopPollingCommand command);
        Task SetPriceAsync(Guid commandId, Dictionary<string, decimal> prices);

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
        Task ResumeFuelingAsync(string groupName);

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
        /// Получить счетчики
        /// </summary>
        /// <param name="groupName">Название группы</param>
        Task GetCountersAsync(Guid commandId, string groupName);

        /// <summary>
        /// Поднята или опущена колонка
        /// </summary>
        Task ColumnLiftedChanged(string groupName, bool isLifted);

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

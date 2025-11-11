using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.Web.Hubs
{
    public interface IDeviceResponseClient
    {
        // Сильная типизация на сервере (по желанию)
        Task StatusChanged(ControllerResponse e);
        Task WorkerStateChanged(string controllerName, string columnName);
        Task StartPolling(StartPollingCommand command);
        Task StopPolling(StopPollingCommand command);
        Task SetPriceAsync(string groupName, decimal price);

        /// <summary>
        /// Начать заправку по сумме или литрам
        /// </summary>
        /// <param name="groupName">Название группы</param>
        /// <param name="sum">Сумма</param>
        /// <param name="bySum">Если true то по сумма, иначе по литражу</param>
        Task StartRefuelingAsync(string groupName, decimal sum, bool bySum);

        /// <summary>
        /// Завершить заправку
        /// </summary>
        /// <param name="groupName">Название группы</param>
        Task CompleteRefuelingAsync(string groupName);

        /// <summary>
        /// Получить счетчики
        /// </summary>
        /// <param name="groupName">Название группы</param>
        Task GetCountersAsync(string groupName);

        /// <summary>
        /// Поднята или опущена колонка
        /// </summary>
        Task ColumnLiftedChanged(string groupName, bool isLifted);
    }
}

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
    }
}

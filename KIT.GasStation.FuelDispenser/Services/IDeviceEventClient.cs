using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.FuelDispenser.Services
{
    /// <summary>
    /// События/уведомления, которые сервер отправляет UI-клиентам.
    /// </summary>
    public interface IDeviceEventClient
    {
        Task StatusChanged(StatusResponse response);

        Task ColumnLiftedChanged(string groupName, bool isLifted);

        Task CountersUpdated(string groupName, IReadOnlyCollection<CounterData> counterDatas);

        Task CounterUpdated(CounterData counterData);

        Task FuelingAsync(FuelingResponse response);

        Task CompletedFuelingAsync(string groupName, decimal? quantity = null);

        Task WaitingAsync(string groupName);

        Task PumpStopAsync(FuelingResponse response);

        Task WorkerStateChanged(WorkerStateNotification notification);
    }
}

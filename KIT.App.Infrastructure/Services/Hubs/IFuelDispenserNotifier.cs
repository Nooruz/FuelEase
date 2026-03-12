using KIT.GasStation.FuelDispenser.Models;

namespace KIT.App.Infrastructure.Services.Hubs
{
    public interface IFuelDispenserNotifier
    {
        Task PublishStatusChangedAsync(StatusResponse response, CancellationToken cancellationToken = default);
        Task PublishColumnLiftedChangedAsync(string groupName, bool isLifted, CancellationToken cancellationToken = default);
        Task PublishWorkerStateChangedAsync(WorkerStateNotification notification, CancellationToken cancellationToken = default);
        Task PublishCountersUpdatedAsync(string groupName, List<CounterData> counters, CancellationToken cancellationToken = default);
        Task PublishCounterUpdatedAsync(CounterData counter, CancellationToken cancellationToken = default);
        Task PublishCompletedFuelingAsync(string groupName, decimal? quantity, CancellationToken cancellationToken = default);
        Task PublishWaitingAsync(string groupName, CancellationToken cancellationToken = default);
        Task PublishPumpStopAsync(FuelingResponse response, CancellationToken cancellationToken = default);
        Task PublishFuelingAsync(FuelingResponse response, CancellationToken cancellationToken = default);
        Task PublishErrorAsync(string groupName, string message, CancellationToken cancellationToken = default);
    }
}

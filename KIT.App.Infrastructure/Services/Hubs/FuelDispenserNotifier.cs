using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using Microsoft.Extensions.Logging;

namespace KIT.App.Infrastructure.Services.Hubs
{
    public sealed class FuelDispenserNotifier : IFuelDispenserNotifier
    {
        private readonly IHubClient _hubClient;
        private readonly ILogger<FuelDispenserNotifier> _logger;

        public FuelDispenserNotifier(
            IHubClient hubClient,
            ILogger<FuelDispenserNotifier> logger)
        {
            _hubClient = hubClient;
            _logger = logger;
        }

        public async Task PublishStatusChangedAsync(StatusResponse response, CancellationToken cancellationToken = default)
        {
            await SendAsync("PublishStatus", new object[] { response }, cancellationToken);
        }

        public async Task PublishColumnLiftedChangedAsync(string groupName, bool isLifted, CancellationToken cancellationToken = default)
        {
            await SendAsync("PublishColumnLiftedChanged", new object[] { groupName, isLifted }, cancellationToken);
        }

        public async Task PublishWorkerStateChangedAsync(WorkerStateNotification notification, CancellationToken cancellationToken = default)
        {
            await SendAsync("PublishWorkerStateChanged", new object[] { notification }, cancellationToken);
        }

        public async Task PublishCountersUpdatedAsync(string groupName, List<CounterData> counters, CancellationToken cancellationToken = default)
        {
            await SendAsync("PublishCountersUpdated", new object[] { groupName, counters }, cancellationToken);
        }

        public async Task PublishCounterUpdatedAsync(CounterData counter, CancellationToken cancellationToken = default)
        {
            await SendAsync("PublishCounterUpdated", new object[] { counter }, cancellationToken);
        }

        public async Task PublishCompletedFuelingAsync(string groupName, decimal? quantity, CancellationToken cancellationToken = default)
        {
            await SendAsync("PublishCompletedFueling", new object[] { groupName, quantity }, cancellationToken);
        }

        public async Task PublishWaitingAsync(string groupName, CancellationToken cancellationToken = default)
        {
            await SendAsync("PublishWaiting", new object[] { groupName }, cancellationToken);
        }

        public async Task PublishPumpStopAsync(FuelingResponse response, CancellationToken cancellationToken = default)
        {
            await SendAsync("PublishPumpStop", new object[] { response }, cancellationToken);
        }

        public async Task PublishFuelingAsync(FuelingResponse response, CancellationToken cancellationToken = default)
        {
            await SendAsync("PublishFueling", new object[] { response }, cancellationToken);
        }

        public async Task PublishErrorAsync(string groupName, string message, CancellationToken cancellationToken = default)
        {
            await SendAsync("PublishError", new object[] { groupName, message }, cancellationToken);
        }

        private async Task SendAsync(string methodName, object[] args, CancellationToken cancellationToken)
        {
            await _hubClient.EnsureStartedAsync(cancellationToken);
            //await _hubClient.Connection.InvokeCoreAsync(methodName, args, cancellationToken);

            _logger.LogDebug("Отправлено событие в hub: {Method}", methodName);
        }
    }
}

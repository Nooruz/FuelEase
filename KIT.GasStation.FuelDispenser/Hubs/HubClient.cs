using Microsoft.AspNetCore.SignalR.Client;

namespace KIT.GasStation.FuelDispenser.Hubs
{
    public sealed class HubClient : IHubClient
    {
        private readonly SemaphoreSlim _startLock = new(1, 1);

        public HubConnection Connection { get; }

        public HubClient(HubConnection connection)
        {
            Connection = connection;
        }

        public async Task EnsureStartedAsync(CancellationToken cancellationToken = default)
        {
            if (Connection.State == HubConnectionState.Connected)
                return;

            await _startLock.WaitAsync(cancellationToken);
            try
            {
                if (Connection.State == HubConnectionState.Disconnected)
                {
                    await Connection.StartAsync(cancellationToken);
                }
            }
            finally
            {
                _startLock.Release();
            }
        }
    }
}

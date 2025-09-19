using Microsoft.AspNetCore.SignalR.Client;

namespace KIT.GasStation.FuelDispenser.Hubs
{
    public class HubClient : IHubClient
    {
        #region Private Members

        private readonly HubConnection _hub;
        private int _started;

        #endregion

        #region Public Properties

        public HubConnection Connection => _hub;

        #endregion

        #region Constructors

        public HubClient(HubConnection hub)
        {
            _hub = hub;
        }

        #endregion

        #region Public Voids

        public async Task EnsureStartedAsync(CancellationToken ct = default)
        {
            if (Interlocked.Exchange(ref _started, 1) == 1) return;
            await _hub.StartAsync(ct);
        }

        #endregion
    }
}

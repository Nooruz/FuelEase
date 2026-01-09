using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace KIT.GasStation.FuelDispenser.Hubs
{
    public class HubClient : IHubClient
    {
        #region Private Members

        private readonly HubConnection _hub;
        private int _started;
        private readonly ILogger? _logger = Log.ForContext<HubClient>();

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
            if (_hub.State == HubConnectionState.Connected)
                return;

            if (_hub.State != HubConnectionState.Disconnected)
            {
                // Если соединение в промежуточном состоянии, ждем или останавливаем
                try
                {
                    await _hub.StopAsync(ct);
                }
                catch { /* Игнорируем ошибки остановки */ }
            }

            var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

            try
            {
                await _hub.StartAsync(timeoutCts.Token);
            }
            catch (Exception ex)
            {
                _logger?.Error(ex, "Ошибка запуска HubConnection");
                throw;
            }
        }

        #endregion
    }
}

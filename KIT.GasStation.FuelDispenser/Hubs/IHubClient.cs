using Microsoft.AspNetCore.SignalR.Client;

namespace KIT.GasStation.FuelDispenser.Hubs
{
    /// <summary>
    /// Один singleton-клиент SignalR на весь Worker.
    /// </summary>
    public interface IHubClient
    {
        HubConnection Connection { get; }
        Task EnsureStartedAsync(CancellationToken cancellationToken = default);
    }
}

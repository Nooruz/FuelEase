using Microsoft.AspNetCore.SignalR.Client;

namespace KIT.GasStation.FuelDispenser.Hubs
{
    public interface IHubClient
    {
        HubConnection Connection { get; }
        Task EnsureStartedAsync(CancellationToken cancellationToken = default);
    }
}

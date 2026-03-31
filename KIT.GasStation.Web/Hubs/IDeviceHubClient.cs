using KIT.GasStation.FuelDispenser.Services;

namespace KIT.GasStation.Worker.Hubs
{
    /// <summary>
    /// Полный контракт клиентской стороны SignalR.
    /// Один интерфейс удобен для Hub<T>.
    /// </summary>
    public interface IDeviceHubClient : IDeviceCommandClient, IDeviceEventClient
    {

    }
}

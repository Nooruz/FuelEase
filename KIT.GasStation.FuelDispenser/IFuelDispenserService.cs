using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.FuelDispenser
{
    public interface IFuelDispenserService : IAsyncDisposable, IDeviceCommandClient
    {
        Controller Controller { get; set; }

        #region Public Voids

        Task RunAsync(CancellationToken token);

        #endregion
    }
}

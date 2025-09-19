using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace KIT.GasStation.Common.Factories
{
    public interface IFuelDispenserFactory
    {
        public IFuelDispenserService Create(IServiceProvider sp, Controller controller, int address, IHubContext<DeviceResponseHub, IDeviceResponseClient> hub);
    }
}

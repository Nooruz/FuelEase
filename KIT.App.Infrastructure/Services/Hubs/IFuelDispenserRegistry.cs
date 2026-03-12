using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.App.Infrastructure.Services.Hubs
{
    public interface IFuelDispenserRegistry
    {
        IFuelDispenserService? GetByGroup(string groupName);
        void Register(IFuelDispenserService dispenser, Controller controller);
    }
}

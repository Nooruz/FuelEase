using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using Serilog;

namespace KIT.GasStation.Common.Factories
{
    public interface IFuelDispenserFactory
    {
        public IFuelDispenserService Create(IServiceProvider sp, Controller controller, int address);
    }
}

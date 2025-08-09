using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.Common.Factories
{
    public interface IFuelDispenserFactory
    {
        public IFuelDispenserService Create(ControllerType controllerType);
    }
}

using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.App.Infrastructure.Factories
{
    public interface IFuelDispenserFactory
    {
        //public IFuelDispenserService Create(IServiceProvider sp, Controller controller, int address, ISharedSerialPortService port);
        //public IFuelDispenserService Create(IServiceProvider sp, Controller controller, ISharedSerialPortService port);
        //public IFuelDispenserService Create(IServiceProvider sp, Controller controller);
        public IFuelDispenserService Create(ControllerType type);
    }
}

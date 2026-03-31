using KIT.GasStation.FuelDispenser;
using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.App.Infrastructure.Factories
{
    /// <summary>
    /// Фабрика создаёт НОВЫЙ экземпляр сервиса ТРК.
    /// Не хранит внутри один singleton-объект.
    /// </summary>
    public interface IFuelDispenserFactory
    {
        //public IFuelDispenserService Create(IServiceProvider sp, Controller controller, int address, ISharedSerialPortService port);
        //public IFuelDispenserService Create(IServiceProvider sp, Controller controller, ISharedSerialPortService port);
        //public IFuelDispenserService Create(IServiceProvider sp, Controller controller);
        public IFuelDispenserService Create(ControllerType type);
    }
}

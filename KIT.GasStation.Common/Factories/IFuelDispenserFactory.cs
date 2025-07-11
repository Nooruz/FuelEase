using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.Common.Factories
{
    public interface IFuelDispenserFactory
    {
        public IFuelDispenserService Create(ControllerType controllerType, IEnumerable<Nozzle>? nozzles = null);
        public Task<IFuelDispenserService?> CreateAsync(IEnumerable<Nozzle> nozzles);
    }
}

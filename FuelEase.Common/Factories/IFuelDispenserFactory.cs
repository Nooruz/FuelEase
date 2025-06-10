using FuelEase.Domain.Models;
using FuelEase.FuelDispenser.Services;
using FuelEase.HardwareConfigurations.Models;

namespace FuelEase.Common.Factories
{
    public interface IFuelDispenserFactory
    {
        public IFuelDispenserService Create(ControllerType controllerType, IEnumerable<Nozzle>? nozzles = null);
        public Task<IFuelDispenserService?> CreateAsync(IEnumerable<Nozzle> nozzles);
    }
}

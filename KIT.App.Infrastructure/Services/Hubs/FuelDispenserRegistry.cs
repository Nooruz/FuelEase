using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.App.Infrastructure.Services.Hubs
{
    public sealed class FuelDispenserRegistry : IFuelDispenserRegistry
    {
        private readonly Dictionary<string, IFuelDispenserService> _map =
        new(StringComparer.Ordinal);

        public void Register(IFuelDispenserService dispenser, Controller controller)
        {
            foreach (var column in controller.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.GroupName))
                    continue;

                _map[column.GroupName] = dispenser;
            }
        }

        public IFuelDispenserService? GetByGroup(string groupName)
        {
            _map.TryGetValue(groupName, out var dispenser);
            return dispenser;
        }
    }
}

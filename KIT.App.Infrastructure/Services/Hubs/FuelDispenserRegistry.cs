using KIT.GasStation.FuelDispenser;
using System.Collections.Concurrent;

namespace KIT.App.Infrastructure.Services.Hubs
{
    public sealed class FuelDispenserRegistry : IFuelDispenserRegistry
    {
        private readonly ConcurrentDictionary<string, IFuelDispenserService> _map =
        new(StringComparer.Ordinal);

        public void Register(IFuelDispenserService dispenser)
        {
            if (dispenser is null)
                throw new ArgumentNullException(nameof(dispenser));

            foreach (var column in dispenser.Controller.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.GroupName))
                    continue;

                _map[column.GroupName] = dispenser;
            }
        }

        public bool Remove(IFuelDispenserService dispenser)
        {
            if (dispenser is null)
                throw new ArgumentNullException(nameof(dispenser));

            foreach (var column in dispenser.Controller.Columns)
            {
                if (string.IsNullOrWhiteSpace(column.GroupName))
                    continue;

                _map.TryRemove(column.GroupName, out _);
            }

            return true;
        }

        public IReadOnlyCollection<string> GetAllGroups() =>
        _map.Keys.ToArray();

        public IFuelDispenserService? GetByGroup(string groupName)
        {
            _map.TryGetValue(groupName, out var dispenser);
            return dispenser;
        }
    }
}

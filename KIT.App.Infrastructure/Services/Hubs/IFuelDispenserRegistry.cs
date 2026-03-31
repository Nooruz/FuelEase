using KIT.GasStation.FuelDispenser;

namespace KIT.App.Infrastructure.Services.Hubs
{
    /// <summary>
    /// Потокобезопасный реестр активных ТРК.
    /// Нужен Router'у, чтобы быстро найти нужный контроллер по groupName.
    /// </summary>
    public interface IFuelDispenserRegistry
    {
        IFuelDispenserService? GetByGroup(string groupName);
        void Register(IFuelDispenserService dispenser);
        bool Remove(IFuelDispenserService dispenser);
        IReadOnlyCollection<string> GetAllGroups();
    }
}

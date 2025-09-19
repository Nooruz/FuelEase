using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.FuelDispenser.Hubs
{
    public interface IDeviceResponseClient
    {
        // Сильная типизация на сервере (по желанию)
        Task StatusChanged(DeviceResponse e);
    }
}

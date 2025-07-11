using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
{
    public interface ITankShiftCounterService : IDataService<TankShiftCounter>
    {
        Task<TankShiftCounter> GetAsync(int tankId, int shiftId);
    }
}

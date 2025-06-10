using FuelEase.Domain.Models;

namespace FuelEase.Domain.Services
{
    public interface ITankShiftCounterService : IDataService<TankShiftCounter>
    {
        Task<TankShiftCounter> GetAsync(int tankId, int shiftId);
    }
}

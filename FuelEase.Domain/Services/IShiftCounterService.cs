using FuelEase.Domain.Models;

namespace FuelEase.Domain.Services
{
    public interface IShiftCounterService : IDataService<ShiftCounter>
    {
        Task<ShiftCounter> GetAsync(int nozzleId, int shiftId);
    }
}

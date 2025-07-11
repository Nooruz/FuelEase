using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
{
    public interface IShiftCounterService : IDataService<ShiftCounter>
    {
        Task<ShiftCounter> GetAsync(int nozzleId, int shiftId);
    }
}

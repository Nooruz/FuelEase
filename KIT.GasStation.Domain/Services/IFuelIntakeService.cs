using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
{
    public interface IFuelIntakeService : IDataService<FuelIntake>
    {
        Task<IEnumerable<FuelIntake>> GetAllAsync(int shiftId);
    }
}

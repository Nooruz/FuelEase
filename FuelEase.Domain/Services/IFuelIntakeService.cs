using FuelEase.Domain.Models;

namespace FuelEase.Domain.Services
{
    public interface IFuelIntakeService : IDataService<FuelIntake>
    {
        Task<IEnumerable<FuelIntake>> GetAllAsync(int shiftId);
    }
}

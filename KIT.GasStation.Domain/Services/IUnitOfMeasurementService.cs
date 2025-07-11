using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
{
    public interface IUnitOfMeasurementService
    {
        Task<IEnumerable<UnitOfMeasurement>> GetAllAsync();
    }
}

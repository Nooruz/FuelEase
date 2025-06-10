using FuelEase.Domain.Models;

namespace FuelEase.Domain.Services
{
    public interface IUnitOfMeasurementService
    {
        Task<IEnumerable<UnitOfMeasurement>> GetAllAsync();
    }
}

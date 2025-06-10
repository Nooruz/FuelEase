using FuelEase.Domain.Models;

namespace FuelEase.Domain.Services
{
    public interface IFuelService : IDataService<Fuel>
    {
        Task<IEnumerable<Fuel>> GetAllByUnitOfMeasurementAsync();
    }
}

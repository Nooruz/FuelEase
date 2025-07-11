using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
{
    public interface IFuelService : IDataService<Fuel>
    {
        Task<IEnumerable<Fuel>> GetAllByUnitOfMeasurementAsync();
    }
}

using FuelEase.Domain.Models;

namespace FuelEase.Domain.Services
{
    public interface INozzleService : IDataService<Nozzle>
    {
        Task<int> GetCountAsync();
    }
}

using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
{
    public interface INozzleService : IDataService<Nozzle>
    {
        Task<int> GetCountAsync();
    }
}

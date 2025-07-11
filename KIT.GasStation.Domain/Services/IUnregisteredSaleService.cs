using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
{
    public interface IUnregisteredSaleService : IDataService<UnregisteredSale>
    {
        Task<IEnumerable<UnregisteredSale>> GetUnregisteredSales();
        Task<IEnumerable<UnregisteredSale>> GetAllAsync(int nozzleId, int shiftId);
    }
}

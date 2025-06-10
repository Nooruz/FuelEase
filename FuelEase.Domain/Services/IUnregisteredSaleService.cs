using FuelEase.Domain.Models;

namespace FuelEase.Domain.Services
{
    public interface IUnregisteredSaleService : IDataService<UnregisteredSale>
    {
        Task<IEnumerable<UnregisteredSale>> GetUnregisteredSales();
        Task<IEnumerable<UnregisteredSale>> GetAllAsync(int nozzleId, int shiftId);
    }
}

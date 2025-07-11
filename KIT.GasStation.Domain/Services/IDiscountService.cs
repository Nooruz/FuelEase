using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
{
    public interface IDiscountService : IDataService<Discount>
    {
        Task<Discount?> GetActiveDiscountAsync();
    }
}

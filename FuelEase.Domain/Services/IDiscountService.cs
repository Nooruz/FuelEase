using FuelEase.Domain.Models;

namespace FuelEase.Domain.Services
{
    public interface IDiscountService : IDataService<Discount>
    {
        Task<Discount?> GetActiveDiscountAsync();
    }
}

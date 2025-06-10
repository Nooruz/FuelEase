using FuelEase.Domain.Views;

namespace FuelEase.Domain.Services
{
    public interface IViewService<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
    }
}

using KIT.GasStation.Domain.Views;

namespace KIT.GasStation.Domain.Services
{
    public interface IViewService<T>
    {
        Task<IEnumerable<T>> GetAllAsync();
    }
}

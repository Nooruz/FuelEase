using KIT.GasStation.Domain.Models;
using System.Security.Principal;

namespace KIT.GasStation.Domain.Services
{
    public interface IShiftService : IDataService<Shift>
    {
        Task<Shift> GetOpenShiftAsync();
        Task<Shift> GetOpenShiftAsync(int userId);
        Task<IEnumerable<Shift>> GetAllAsync(int userId);
    }
}

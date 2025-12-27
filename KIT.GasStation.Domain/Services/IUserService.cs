using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
{
    public interface IUserService : IDataService<User>
    {
        Task<User> GetByUsername(string username);
        Task<bool> AnyAsync();
        IEnumerable<User> GetCashiers();
        Task<IEnumerable<User>> GetAdminAsync();
        Task<IEnumerable<User>> GetCashiersAsync();
    }
}

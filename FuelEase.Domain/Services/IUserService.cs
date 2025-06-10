using FuelEase.Domain.Models;

namespace FuelEase.Domain.Services
{
    public interface IUserService : IDataService<User>
    {
        Task<User> GetByUsername(string username);
        Task<bool> AnyAsync();
        IEnumerable<User> GetCashiers();
        Task<IEnumerable<User>> GetAdminAsync();
        Task<IEnumerable<User>> GetCashiersAsync();

        /// <summary>
        /// Поментка на удаление
        /// </summary>
        /// <param name="user">Пользователь</param>
        /// <returns></returns>
        Task<bool> MarkingForDeletion(User user);
    }
}

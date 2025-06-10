using FuelEase.Domain.Models;

namespace FuelEase.Domain.Services
{
    public interface ITankService : IDataService<Tank>
    {
        /// <summary>
        /// Свободен ли код резервуара
        /// </summary>
        /// <param name="number">Код резервуара</param>
        Task<bool> IsNumberAvailableAsync(int number);
    }
}

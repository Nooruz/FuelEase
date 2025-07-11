using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
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

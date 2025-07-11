using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Domain.Services
{
    public interface IEventPanelService : IDataService<EventPanel>, IDisposable
    {
        Task<IEnumerable<EventPanel>> GetAllAsync(int shiftId);

        /// <summary>
        /// Ставим запись в очередь, не дожидаясь завершения
        /// </summary>
        /// <param name="eventPanel"></param>
        /// <returns></returns>
        ValueTask EnqueueUpdateAsync(EventPanel eventPanel);
    }
}

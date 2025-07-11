using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;
using System.Threading.Channels;

namespace KIT.GasStation.EntityFramework.Services
{
    public class EventPanelService : IEventPanelService
    {
        #region Private Members

        // Канал для буферизации запросов на обновление
        private readonly Channel<EventPanel> _updateChannel = Channel.CreateUnbounded<EventPanel>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );
        private readonly Task _processorTask;
        private GasStationDbContextFactory _contextFactory;
        private readonly NonQueryDataService<EventPanel> _nonQueryDataService;

        public event Action<EventPanel> OnCreated;
        public event Action<EventPanel> OnUpdated;
        public event Action<int> OnDeleted;

        #endregion

        #region Constructor

        public EventPanelService(GasStationDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<EventPanel>(_contextFactory);

            // Запускаем фоновый процесс «потребления» канала
            _processorTask = Task.Run(ProcessUpdateQueueAsync);
        }

        #endregion

        public ValueTask EnqueueUpdateAsync(EventPanel eventPanel)
        {
            // Всегда пишем текущее состояние
            _updateChannel.Writer.TryWrite(eventPanel);

            return default;
        }

        public async Task<EventPanel> CreateAsync(EventPanel entity)
        {
            var result = await _nonQueryDataService.Create(entity);
            if (result != null)
                OnCreated?.Invoke(result);
            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _nonQueryDataService.Delete(id);
            if (result)
                OnDeleted?.Invoke(id);
            return result;
        }

        public async Task<IEnumerable<EventPanel>> GetAllAsync(int shiftId)
        {
            try
            {
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
                return await context.EventsPanel
                    .Where(e => e.ShiftId == shiftId)
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<IEnumerable<EventPanel>> GetAllAsync()
        {
            try
            {
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
                return await context.EventsPanel
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<EventPanel> GetAsync(int id)
        {
            try
            {
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
                return await context.EventsPanel
                    .FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<EventPanel> UpdateAsync(int id, EventPanel entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(result);
            return result;
        }

        public void Dispose()
        {
            _updateChannel.Writer.Complete();
            _processorTask.Wait(); // или await, если позволено
        }

        #region Private Voids

        private async Task ProcessUpdateQueueAsync()
        {
            var reader = _updateChannel.Reader;
            while (await reader.WaitToReadAsync())
            {
                while (reader.TryRead(out var eventPanel))
                {
                    try
                    {
                        // здесь действительно вызываем UpdateAsync
                        await UpdateAsync(eventPanel.Id, eventPanel);
                    }
                    catch (Exception ex)
                    {
                        // логируем ex, или помещаем обратно в канал
                    }
                }
            }
        }

        #endregion
    }
}

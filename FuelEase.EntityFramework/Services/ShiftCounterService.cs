using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class ShiftCounterService : IShiftCounterService
    {
        #region Private Members

        private FuelEaseDbContextFactory _contextFactory;
        private readonly NonQueryDataService<ShiftCounter> _nonQueryDataService;

        public event Action<ShiftCounter> OnCreated;
        public event Action<ShiftCounter> OnUpdated;
        public event Action<int> OnDeleted;

        #endregion

        #region Constructor

        public ShiftCounterService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<ShiftCounter>(_contextFactory);
        }

        #endregion

        public async Task<ShiftCounter> CreateAsync(ShiftCounter entity)
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

        public async Task<IEnumerable<ShiftCounter>> GetAllAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.ShiftCounters
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<ShiftCounter> GetAsync(int id)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.ShiftCounters
                    .FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<ShiftCounter> GetAsync(int nozzleId, int shiftId)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.ShiftCounters
                    .FirstOrDefaultAsync((e) => e.NozzleId == nozzleId && e.ShiftId == shiftId);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<ShiftCounter> UpdateAsync(int id, ShiftCounter entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(result);
            return result;
        }
    }
}

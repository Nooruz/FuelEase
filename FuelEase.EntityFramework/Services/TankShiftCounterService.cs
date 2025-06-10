using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class TankShiftCounterService : ITankShiftCounterService
    {
        #region Private Members

        private FuelEaseDbContextFactory _contextFactory;
        private readonly NonQueryDataService<TankShiftCounter> _nonQueryDataService;

        public event Action<TankShiftCounter> OnCreated;
        public event Action<TankShiftCounter> OnUpdated;
        public event Action<int> OnDeleted;

        #endregion

        #region Constructor

        public TankShiftCounterService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<TankShiftCounter>(_contextFactory);
        }

        #endregion

        public async Task<TankShiftCounter> CreateAsync(TankShiftCounter entity)
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

        public async Task<IEnumerable<TankShiftCounter>> GetAllAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.TankShiftCounters
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<TankShiftCounter> GetAsync(int id)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.TankShiftCounters
                    .FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<TankShiftCounter> GetAsync(int tankId, int shiftId)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.TankShiftCounters
                    .FirstOrDefaultAsync((e) => e.TankId == tankId && e.ShiftId == shiftId);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<TankShiftCounter> UpdateAsync(int id, TankShiftCounter entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(result);
            return result;
        }
    }
}

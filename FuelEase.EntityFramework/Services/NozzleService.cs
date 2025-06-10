using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class NozzleService : INozzleService
    {
        #region Private Members

        private FuelEaseDbContextFactory _contextFactory;
        private readonly NonQueryDataService<Nozzle> _nonQueryDataService;

        public event Action<Nozzle> OnCreated;
        public event Action<Nozzle> OnUpdated;
        public event Action<int> OnDeleted;

        #endregion

        #region Constructor

        public NozzleService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<Nozzle>(_contextFactory);
        }

        #endregion

        public async Task<Nozzle> CreateAsync(Nozzle entity)
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

        public async Task<Nozzle> GetAsync(int id)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Nozzles
                    .FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<IEnumerable<Nozzle>> GetAllAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Nozzles
                    .Include(n => n.Tank)
                    .ThenInclude(n => n.Fuel)
                    .ThenInclude(n => n.UnitOfMeasurement)
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<int> GetCountAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Nozzles.CountAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return 0;
        }

        public async Task<Nozzle> UpdateAsync(int id, Nozzle entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(result);
            return result;
        }
    }
}
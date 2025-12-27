using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.EntityFramework.Services
{
    public class NozzleService : INozzleService
    {
        #region Private Members

        private GasStationDbContextFactory _contextFactory;
        private readonly NonQueryDataService<Nozzle> _nonQueryDataService;

        public event Action<Nozzle> OnCreated;
        public event Action<Nozzle> OnUpdated;
        public event Action<int> OnDeleted;

        #endregion

        #region Constructor

        public NozzleService(GasStationDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<Nozzle>(_contextFactory);
        }

        #endregion

        public async Task<Nozzle> CreateAsync(Nozzle entity)
        {
            entity.CreatedAt = DateTime.Now;
            var result = await _nonQueryDataService.Create(entity);
            if (result != null)
                OnCreated?.Invoke(await GetAsync(result.Id));
            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await GetAsync(id);

            if (entity == null) return false;

            entity.DeletedAt = DateTime.Now;
            entity.IsDeleted = true;

            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnDeleted?.Invoke(id);
            return result != null;
        }

        public async Task<Nozzle> GetAsync(int id)
        {
            try
            {
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
                return await context.Nozzles
                    .Where(n => !n.IsDeleted)
                    .Include(n => n.Tank)
                    .ThenInclude(n => n.Fuel)
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
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
                return await context.Nozzles
                    .Where(n => !n.IsDeleted)
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
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
                return await context.Nozzles.Where(n => !n.IsDeleted).CountAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return 0;
        }

        public async Task<Nozzle> UpdateAsync(int id, Nozzle entity)
        {
            entity.UpdatedAt = DateTime.Now;
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(result);
            return result;
        }
    }
}
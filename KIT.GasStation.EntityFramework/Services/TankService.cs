using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.EntityFramework.Services
{
    public class TankService : ITankService
    {
        private readonly GasStationDbContextFactory _contextFactory;
        private readonly NonQueryDataService<Tank> _nonQueryDataService;

        public TankService(GasStationDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<Tank>(_contextFactory);
        }

        public event Action<Tank> OnCreated;
        public event Action<Tank> OnUpdated;
        public event Action<int> OnDeleted;

        public async Task<Tank> CreateAsync(Tank entity)
        {
            entity.CreatedAt = DateTime.Now;
            var result = await _nonQueryDataService.Create(entity);
            if (result != null)
            {
                OnCreated?.Invoke(await GetAsync(result.Id));
            }
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
            {
                OnDeleted?.Invoke(id);
            }
            return result != null;
        }

        public async Task<IEnumerable<Tank>> GetAllAsync()
        {
            try
            {
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
                return await context.Tanks
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.Fuel)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Tank>();
                //ignore
            }
        }

        public async Task<Tank> GetAsync(int id)
        {
            try
            {
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
                return await context.Tanks
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.Fuel)
                    .FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                return new Tank();
                //ignore
            }
        }

        ///<inheritdoc/>
        public async Task<bool> IsNumberAvailableAsync(Tank tank)
        {
            try
            {
                await using GasStationDbContext context = _contextFactory.CreateDbContext();

                var tanks = await context.Tanks
                    .Where(t => !t.IsDeleted && t.Number == tank.Number)
                    .ToListAsync();

                if (tanks == null) return false;

                return tanks.Any(t => t.Id != tank.Id);
            }
            catch (Exception)
            {
                return false;
                //ignore
            }
        }

        public async Task<Tank> UpdateAsync(int id, Tank entity)
        {
            entity.UpdatedAt = DateTime.Now;
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(await GetAsync(result.Id));
            return result;
        }
    }
}

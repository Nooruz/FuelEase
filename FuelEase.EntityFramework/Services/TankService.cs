using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class TankService : ITankService
    {
        private readonly FuelEaseDbContextFactory _contextFactory;
        private readonly NonQueryDataService<Tank> _nonQueryDataService;

        public TankService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<Tank>(_contextFactory);
        }

        public event Action<Tank> OnCreated;
        public event Action<Tank> OnUpdated;
        public event Action<int> OnDeleted;

        public async Task<Tank> CreateAsync(Tank entity)
        {
            var result = await _nonQueryDataService.Create(entity);
            if (result != null)
            {
                OnCreated?.Invoke(await GetAsync(result.Id));
            }
            return result;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _nonQueryDataService.Delete(id);
            if (result)
            {
                OnDeleted?.Invoke(id);
            }
            return result;
        }

        public async Task<IEnumerable<Tank>> GetAllAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Tanks.Include(t => t.Fuel).ToListAsync();
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
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Tanks.Include(t => t.Fuel).FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                return new Tank();
                //ignore
            }
        }

        ///<inheritdoc/>
        public async Task<bool> IsNumberAvailableAsync(int number)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                Tank? tank = await context.Tanks.FirstOrDefaultAsync((e) => e.Number == number);

                return tank != null;
            }
            catch (Exception)
            {
                return false;
                //ignore
            }
        }

        public async Task<Tank> UpdateAsync(int id, Tank entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(await GetAsync(result.Id));
            return result;
        }
    }
}

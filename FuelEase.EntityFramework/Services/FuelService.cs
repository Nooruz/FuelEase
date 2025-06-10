using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class FuelService : IFuelService
    {
        private readonly FuelEaseDbContextFactory _contextFactory;
        private readonly NonQueryDataService<Fuel> _nonQueryDataService;

        public FuelService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<Fuel>(_contextFactory);
        }

        public event Action<Fuel> OnCreated;
        public event Action<Fuel> OnUpdated;
        public event Action<int> OnDeleted;

        public async Task<Fuel> CreateAsync(Fuel entity)
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

        public async Task<IEnumerable<Fuel>> GetAllAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Fuels.Include(f => f.Tanks).ToListAsync();
            }
            catch (Exception)
            {
                return new List<Fuel>();
                //ignore
            }
        }

        public async Task<IEnumerable<Fuel>> GetAllByUnitOfMeasurementAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Fuels
                    .Include(f => f.Tanks)
                    .Include(f => f.UnitOfMeasurement)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<Fuel>();
                //ignore
            }
        }

        public async Task<Fuel> GetAsync(int id)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Fuels.Include(f => f.Tanks).FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                return new Fuel();
                //ignore
            }
        }

        public async Task<Fuel> UpdateAsync(int id, Fuel entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(await GetAsync(result.Id));
            return result;
        }
    }
}

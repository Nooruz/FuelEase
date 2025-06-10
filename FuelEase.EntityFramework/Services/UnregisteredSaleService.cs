using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class UnregisteredSaleService : IUnregisteredSaleService
    {
        #region Private Members

        private readonly FuelEaseDbContextFactory _contextFactory;
        private readonly NonQueryDataService<UnregisteredSale> _nonQueryDataService;

        #endregion

        #region Public Properties

        public event Action<UnregisteredSale> OnCreated;
        public event Action<UnregisteredSale> OnUpdated;
        public event Action<int> OnDeleted;

        #endregion

        #region Constructor

        public UnregisteredSaleService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<UnregisteredSale>(_contextFactory);
        }

        #endregion

        public async Task<UnregisteredSale> CreateAsync(UnregisteredSale entity)
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

        public async Task<IEnumerable<UnregisteredSale>> GetAllAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.UnregisteredSales
                    .Include(u => u.Nozzle)
                    .ThenInclude(u => u.Tank)
                    .ThenInclude(u => u.Fuel)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<UnregisteredSale>();
                //ignore
            }
        }

        public async Task<IEnumerable<UnregisteredSale>> GetAllAsync(int nozzleId, int shiftId)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.UnregisteredSales
                    .Where(u => u.NozzleId == nozzleId && u.ShiftId == shiftId)
                    .Include(u => u.Nozzle)
                    .ThenInclude(u => u.Tank)
                    .ThenInclude(u => u.Fuel)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<UnregisteredSale>();
                //ignore
            }
        }

        public async Task<IEnumerable<UnregisteredSale>> GetUnregisteredSales()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.UnregisteredSales
                    .Where(u => u.State == UnregisteredSaleState.Waiting)
                    .Include(u => u.Nozzle)
                        .ThenInclude(u => u.Tank)
                            .ThenInclude(u => u.Fuel)
                    .Include(u => u.Nozzle)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<UnregisteredSale>();
                //ignore
            }
        }

        public async Task<UnregisteredSale> GetAsync(int id)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.UnregisteredSales
                    .Include(u => u.Nozzle)
                    .ThenInclude(u => u.Tank)
                    .ThenInclude(u => u.Fuel)
                    .FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                return new UnregisteredSale();
                //ignore
            }
        }

        public async Task<UnregisteredSale> UpdateAsync(int id, UnregisteredSale entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(await GetAsync(result.Id));
            return result;
        }
        
        public async Task<ICollection<UnregisteredSale>> GetUnregisteredSales(int nozzleId)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.UnregisteredSales
                    .Where(u => u.NozzleId == nozzleId)
                    .ToListAsync();
            }
            catch (Exception)
            {
                return new List<UnregisteredSale>();
                //ignore
            }
        }
    }
}

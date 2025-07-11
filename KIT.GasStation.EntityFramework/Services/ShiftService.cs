using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.EntityFramework.Services
{
    public class ShiftService : IShiftService
    {
        private readonly GasStationDbContextFactory _contextFactory;
        private readonly NonQueryDataService<Shift> _nonQueryDataService;

        public ShiftService(GasStationDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<Shift>(_contextFactory);
        }

        public event Action<Shift> OnCreated;
        public event Action<Shift> OnUpdated;
        public event Action<int> OnDeleted;

        public async Task<Shift> CreateAsync(Shift entity)
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

        public async Task<IEnumerable<Shift>> GetAllAsync()
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Shifts
                    .Include(s => s.User)
                    .Include(s => s.FuelSales)
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<IEnumerable<Shift>> GetAllAsync(int userId)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Shifts
                    .Where(s => s.UserId == userId)
                    .Include(s => s.FuelSales)
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<Shift> GetAsync(int id)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Shifts
                    .Include(s => s.User)
                    .Include(s => s.FuelSales)
                    .FirstOrDefaultAsync(s => s.Id == id);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<Shift> GetOpenShiftAsync()
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Shifts.Include(s => s.User).FirstOrDefaultAsync(s => s.ClosedDate == null);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<Shift> GetOpenShiftAsync(int userId)
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.Shifts.Where(s => s.UserId == userId)
                    .OrderByDescending(s => s.OpeningDate)
                    .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<Shift> UpdateAsync(int id, Shift entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(await GetAsync(result.Id));
            return result;
        }
    }
}

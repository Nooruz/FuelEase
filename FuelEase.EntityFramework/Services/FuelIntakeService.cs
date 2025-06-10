using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class FuelIntakeService : IFuelIntakeService
    {
        #region Private Members

        private FuelEaseDbContextFactory _contextFactory;
        private readonly NonQueryDataService<FuelIntake> _nonQueryDataService;

        public event Action<FuelIntake> OnCreated;
        public event Action<FuelIntake> OnUpdated;
        public event Action<int> OnDeleted;

        #endregion

        #region Constructor

        public FuelIntakeService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<FuelIntake>(_contextFactory);
        }

        #endregion

        public async Task<FuelIntake> CreateAsync(FuelIntake entity)
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

        public async Task<IEnumerable<FuelIntake>> GetAllAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.FuelIntakes
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<IEnumerable<FuelIntake>> GetAllAsync(int shiftId)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.FuelIntakes
                    .Where(f => f.ShiftId == shiftId)
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<FuelIntake> GetAsync(int id)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.FuelIntakes
                    .FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<FuelIntake> UpdateAsync(int id, FuelIntake entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(result);
            return result;
        }
    }
}

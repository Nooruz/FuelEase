using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.EntityFramework.Services
{
    public class FuelIntakeService : IFuelIntakeService
    {
        #region Private Members

        private GasStationDbContextFactory _contextFactory;
        private readonly NonQueryDataService<FuelIntake> _nonQueryDataService;

        public event Action<FuelIntake> OnCreated;
        public event Action<FuelIntake> OnUpdated;
        public event Action<int> OnDeleted;

        #endregion

        #region Constructor

        public FuelIntakeService(GasStationDbContextFactory contextFactory)
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
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
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
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
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
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
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

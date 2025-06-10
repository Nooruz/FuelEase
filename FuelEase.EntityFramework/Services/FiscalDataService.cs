using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class FiscalDataService : IFiscalDataService
    {
        #region Private Members

        private FuelEaseDbContextFactory _contextFactory;
        private readonly NonQueryDataService<FiscalData> _nonQueryDataService;

        public event Action<FiscalData> OnCreated;
        public event Action<FiscalData> OnUpdated;
        public event Action<int> OnDeleted;

        #endregion

        #region Constructor

        public FiscalDataService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<FiscalData>(_contextFactory);
        }

        #endregion

        public async Task<FiscalData> CreateAsync(FiscalData entity)
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

        public async Task<IEnumerable<FiscalData>> GetAllAsync(int shiftId)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.FiscalDatas
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<IEnumerable<FiscalData>> GetAllAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.FiscalDatas
                    .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<FiscalData> GetAsync(int id)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.FiscalDatas
                    .FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<FiscalData> UpdateAsync(int id, FiscalData entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(result);
            return result;
        }
    }
}

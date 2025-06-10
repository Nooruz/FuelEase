using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class GenericService<T> : IDataService<T> where T : DomainObject
    {
        #region Private Members

        private FuelEaseDbContextFactory _contextFactory;
        private readonly NonQueryDataService<T> _nonQueryDataService;

        public event Action<T> OnCreated;
        public event Action<T> OnUpdated;
        public event Action<int> OnDeleted;

        #endregion

        #region Constructor

        public GenericService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<T>(_contextFactory);
        }

        #endregion

        public async Task<T> CreateAsync(T entity)
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

        public async Task<T> GetAsync(int id)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Set<T>().FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Set<T>().ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<T> UpdateAsync(int id, T entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(result);
            return result;
        }

        public IEnumerable<T> GetAll()
        {
            try
            {
                using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return context.Set<T>().ToList();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }
    }
}

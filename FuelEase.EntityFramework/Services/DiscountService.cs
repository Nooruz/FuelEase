using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.EntityFramework.Services.Common;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class DiscountService : IDiscountService
    {
        #region Private Members

        private FuelEaseDbContextFactory _contextFactory;
        private readonly NonQueryDataService<Discount> _nonQueryDataService;

        public event Action<Discount> OnCreated;
        public event Action<Discount> OnUpdated;
        public event Action<int> OnDeleted;

        #endregion

        #region Constructor

        public DiscountService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
            _nonQueryDataService = new NonQueryDataService<Discount>(_contextFactory);
        }

        #endregion

        public async Task<Discount> CreateAsync(Discount entity)
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

        public async Task<IEnumerable<Discount>> GetAllAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Discounts
                        .Include(d => d.DiscountTariffPlans)
                        .Include(d => d.DiscountFuels)
                        .ToListAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<Discount> GetAsync(int id)
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Discounts
                    .FirstOrDefaultAsync((e) => e.Id == id);
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<Discount?> GetActiveDiscountAsync()
        {
            try
            {
                await using FuelEaseDbContext context = _contextFactory.CreateDbContext();
                return await context.Discounts
                        .Where(d => d.StartDate <= DateTime.Now && d.EndDate >= DateTime.Now)
                        .Include(d => d.DiscountTariffPlans)
                        .Include(d => d.DiscountFuels)
                        .FirstOrDefaultAsync();
            }
            catch (Exception)
            {
                //ignore
            }
            return null;
        }

        public async Task<Discount> UpdateAsync(int id, Discount entity)
        {
            var result = await _nonQueryDataService.Update(id, entity);
            if (result != null)
                OnUpdated?.Invoke(result);
            return result;
        }
    }
}

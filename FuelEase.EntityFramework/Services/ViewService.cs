using FuelEase.Domain.Services;
using FuelEase.Domain.Views;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class ViewService<T> : IViewService<T> where T : ViewObject
    {
        #region Private Members

        private FuelEaseDbContextFactory _contextFactory;

        #endregion

        #region Constructor

        public ViewService(FuelEaseDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        #endregion

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
    }
}

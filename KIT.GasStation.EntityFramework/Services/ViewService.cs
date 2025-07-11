using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Views;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.EntityFramework.Services
{
    public class ViewService<T> : IViewService<T> where T : ViewObject
    {
        #region Private Members

        private GasStationDbContextFactory _contextFactory;

        #endregion

        #region Constructor

        public ViewService(GasStationDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        #endregion

        public async Task<IEnumerable<T>> GetAllAsync()
        {
            try
            {
                await using GasStationDbContext context = _contextFactory.CreateDbContext();
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

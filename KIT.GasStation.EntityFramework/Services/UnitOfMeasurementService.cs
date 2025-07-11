using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.EntityFramework.Services
{
    public class UnitOfMeasurementService : IUnitOfMeasurementService
    {
        private readonly GasStationDbContextFactory _contextFactory;

        public UnitOfMeasurementService(GasStationDbContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public async Task<IEnumerable<UnitOfMeasurement>> GetAllAsync()
        {
            try
            {
                await using var context = _contextFactory.CreateDbContext();
                return await context.UnitOfMeasurements.ToListAsync();
            }
            catch (Exception)
            {
                return new List<UnitOfMeasurement>();
            }
        }
    }
}

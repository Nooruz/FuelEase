using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework.Services
{
    public class UnitOfMeasurementService : IUnitOfMeasurementService
    {
        private readonly FuelEaseDbContextFactory _contextFactory;

        public UnitOfMeasurementService(FuelEaseDbContextFactory contextFactory)
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

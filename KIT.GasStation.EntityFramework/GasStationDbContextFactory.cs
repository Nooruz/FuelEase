using Microsoft.EntityFrameworkCore;

namespace KIT.GasStation.EntityFramework
{
    public class GasStationDbContextFactory
    {
        private readonly Action<DbContextOptionsBuilder> _configureDbContext;
        public GasStationDbContextFactory(Action<DbContextOptionsBuilder> configureDbContext)
        {
            _configureDbContext = configureDbContext;
        }

        public GasStationDbContext CreateDbContext()
        {
            DbContextOptionsBuilder<GasStationDbContext> options = new();

            _configureDbContext(options);

            return new GasStationDbContext(options.Options);
        }
    }
}

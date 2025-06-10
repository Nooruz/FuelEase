using Microsoft.EntityFrameworkCore;

namespace FuelEase.EntityFramework
{
    public class FuelEaseDbContextFactory
    {
        private readonly Action<DbContextOptionsBuilder> _configureDbContext;
        public FuelEaseDbContextFactory(Action<DbContextOptionsBuilder> configureDbContext)
        {
            _configureDbContext = configureDbContext;
        }

        public FuelEaseDbContext CreateDbContext()
        {
            DbContextOptionsBuilder<FuelEaseDbContext> options = new();

            _configureDbContext(options);

            return new FuelEaseDbContext(options.Options);
        }
    }
}

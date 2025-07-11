using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace KIT.GasStation.EntityFramework
{
    public class GasStationDbContextDesignTimeFactory : IDesignTimeDbContextFactory<GasStationDbContext>
    {
        public GasStationDbContext CreateDbContext(string[] args)
        {
            // Загружаем конфигурацию (предполагается, что appsettings.json находится в корне проекта)
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .Build();

            void ConfigureDbContext(DbContextOptionsBuilder o) =>
                o.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

            var factory = new GasStationDbContextFactory(ConfigureDbContext);
            return factory.CreateDbContext();
        }
    }
}

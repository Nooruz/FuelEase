using KIT.GasStation.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.HostBuilders
{
    public static class AddDbContextHostBuilderExtensions
    {
        public static IHostBuilder AddDbContext(this IHostBuilder host)
        {
            return host.ConfigureServices((context, services) =>
            {
                void ConfigureDbContext(DbContextOptionsBuilder o) => o.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection"));
                _ = services.AddDbContext<GasStationDbContext>(ConfigureDbContext);
                _ = services.AddSingleton(new GasStationDbContextFactory(ConfigureDbContext));
                //_ = services.AddSignalR();
            });
        }
    }
}

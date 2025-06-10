using FuelEase.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FuelEase.HostBuilders
{
    public static class AddDbContextHostBuilderExtensions
    {
        public static IHostBuilder AddDbContext(this IHostBuilder host)
        {
            return host.ConfigureServices((context, services) =>
            {
                void ConfigureDbContext(DbContextOptionsBuilder o) => o.UseSqlServer(context.Configuration.GetConnectionString("DefaultConnection"));
                _ = services.AddDbContext<FuelEaseDbContext>(ConfigureDbContext);
                _ = services.AddSingleton(new FuelEaseDbContextFactory(ConfigureDbContext));
                //_ = services.AddSignalR();
            });
        }
    }
}

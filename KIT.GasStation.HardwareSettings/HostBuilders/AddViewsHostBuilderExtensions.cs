using KIT.GasStation.HardwareSettings.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.HardwareSettings.HostBuilders
{
    public static class AddViewsHostBuilderExtensions
    {
        public static IHostBuilder AddViews(this IHostBuilder host)
        {
            return host.ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
            {
                services.AddTransient<Main>();
                services.AddTransient<HardwareDialog>();
                services.AddTransient<ColumnCountDialog>();
            });
        }
    }
}

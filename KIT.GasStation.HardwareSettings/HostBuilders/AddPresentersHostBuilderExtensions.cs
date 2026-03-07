using KIT.GasStation.HardwareSettings.Presenters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.HardwareSettings.HostBuilders
{
    public static class AddPresentersHostBuilderExtensions
    {
        public static IHostBuilder AddPresenters(this IHostBuilder host)
        {
            return host.ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
            {
                services.AddTransient<MainPresenter>();
            });
        }
    }
}

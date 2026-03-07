using KIT.GasStation.HardwareSettings.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.HardwareSettings.HostBuilders
{
    public static class AddLocalServicesHostBuilderExtensions
    {
        public static IHostBuilder AddLocalServices(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                services.AddSingleton<IDialogService, DialogService>();
            });
        }
    }
}

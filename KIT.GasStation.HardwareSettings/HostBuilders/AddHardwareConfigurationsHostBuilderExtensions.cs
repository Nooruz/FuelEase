using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.HardwareSettings.HostBuilders
{
    public static class AddHardwareConfigurationsHostBuilderExtensions
    {
        public static IHostBuilder AddHardwareConfigurationsServices(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                services.AddSingleton<IPortManager, PortManager>();
                services.AddSingleton<ISharedSerialPortService, SharedSerialPortService>();
                services.AddSingleton<IHardwareConfigurationService, HardwareConfigurationService>();
            });
        }
    }
}

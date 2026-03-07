using KIT.App.Infrastructure.Factories;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.App.Infrastructure.HostBuilders
{
    public static class AddHardwareConfigurationsServicesHostBuilderExtensions
    {
        public static IHostBuilder AddHardwareConfigurationsServices(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                services.AddSingleton<IPortManager, PortManager>();
                services.AddSingleton<ISharedSerialPortService, SharedSerialPortService>();
                services.AddSingleton<IHardwareConfigurationService, HardwareConfigurationService>();
                services.AddSingleton<IFuelDispenserFactory, FuelDispenserFactory>();
            });
        }
    }
}

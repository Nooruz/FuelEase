using FuelEase.Common.Factories;
using FuelEase.FuelDispenser.Services.Factories;
using FuelEase.HardwareConfigurations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FuelEase.Common.HostBuilders
{
    public static class AddHardwareConfigurationsServicesHostBuilderExtensions
    {
        public static IHostBuilder AddHardwareConfigurationsServices(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                services.AddSingleton<IPortManager, PortManager>();
                services.AddSingleton<IHardwareConfigurationService, HardwareConfigurationService>();
                services.AddSingleton<ICommandEncoderFactory, CommandEncoderFactory>();
                services.AddSingleton<IProtocolParserFactory, ProtocolParserFactory>();
                services.AddSingleton<IFuelDispenserFactory, FuelDispenserFactory>();
            });
        }
    }
}

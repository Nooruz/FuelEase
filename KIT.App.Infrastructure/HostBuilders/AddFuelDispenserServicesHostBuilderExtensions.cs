using KIT.GasStation.Emulator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.App.Infrastructure.HostBuilders
{
    public static class AddFuelDispenserServicesHostBuilderExtensions
    {
        public static IHostBuilder AddFuelDispenserServices(this IHostBuilder host)
        {
            return host.ConfigureServices((context, services) =>
            {
                services.AddTransient<EmulatorFuelDispenser>();
            });
        }
    }
}
using KIT.GasStation.HardwareConfigurations.Services;

namespace KIT.GasStation.Web.HostBuilders
{
    public static class AddServicesHostBuilderExtensions
    {
        public static IHostBuilder AddServices(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                _ = services.AddSingleton<IHardwareConfigurationService, HardwareConfigurationService>();
            });
        }
    }
}

using FuelEase.Hardware.State.Navigators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FuelEase.Hardware.HostBuilders
{
    public static class AddStoresHostBuilderExtensions
    {
        public static IHostBuilder AddStores(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                _ = services.AddSingleton<INavigator, Navigator>();
            });
        }
    }
}

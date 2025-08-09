using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.Hardware.HostBuilders
{
    public static class AddConfigurationHostBuilderExtensions
    {
        public static IHostBuilder AddConfiguration(this IHostBuilder host)
        {
            return host.ConfigureAppConfiguration(c =>
            {
                c.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                c.AddEnvironmentVariables();
            });
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.HostBuilders
{
    public static class AddConfigurationHostBuilderExtensions
    {
        public static IHostBuilder AddConfiguration(this IHostBuilder host)
        {
            return host.ConfigureAppConfiguration(c =>
            {
                c.AddJsonFile("appsettings.json");
                c.AddEnvironmentVariables();
            });
        }
    }
}

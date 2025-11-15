using Microsoft.Extensions.Configuration;

namespace KIT.GasStation.Mobile.HostBuilders
{
    public static class AddConfigurationHostBuilderExtensions
    {
        public static MauiAppBuilder AddConfiguration(this MauiAppBuilder builder)
        {
            builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables();

            return builder;
        }
    }
}

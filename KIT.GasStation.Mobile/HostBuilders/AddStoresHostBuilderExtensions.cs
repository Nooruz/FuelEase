using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.Mobile.HostBuilders
{
    public static class AddStoresHostBuilderExtensions
    {
        public static MauiAppBuilder AddStores(this MauiAppBuilder builder)
        {
            //builder.Services.AddSingleton<SessionStore>();
            // другие сторы

            return builder;
        }
    }
}

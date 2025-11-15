using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;
using KIT.GasStation.Mobile.HostBuilders;

namespace KIT.GasStation.Mobile
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>()
                .UseMauiCommunityToolkit();

            // Host-цепочка
            builder
                .AddConfiguration()
                .AddPages()
                .AddViewModels()
                .AddStores()
                .AddServices();

#if DEBUG
            builder.Logging.AddDebug();
#endif
            return builder.Build();
        }
    }
}
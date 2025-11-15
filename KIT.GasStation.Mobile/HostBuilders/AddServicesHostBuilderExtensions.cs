using KIT.GasStation.Mobile.ViewModels;

namespace KIT.GasStation.Mobile.HostBuilders
{
    public static class AddServicesHostBuilderExtensions
    {
        public static MauiAppBuilder AddServices(this MauiAppBuilder builder)
        {
            builder.Services.AddTransient<MainPageViewModel>();
            // остальные VM

            return builder;
        }
    }
}

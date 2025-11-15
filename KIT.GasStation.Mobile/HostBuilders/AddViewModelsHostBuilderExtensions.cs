using KIT.GasStation.Mobile.ViewModels;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.Mobile.HostBuilders
{
    public static class AddViewModelsHostBuilderExtensions
    {
        public static MauiAppBuilder AddViewModels(this MauiAppBuilder builder)
        {
            builder.Services.AddTransient<MainPageViewModel>();
            //builder.Services.AddTransient<DetailsViewModel>();
            // остальные VM

            return builder;
        }
    }
}

using KIT.GasStation.HardwareSettings.CustomControl;
using KIT.GasStation.HardwareSettings.CustomControl.Factories;
using KIT.GasStation.HardwareSettings.CustomControl.Pages;
using KIT.GasStation.HardwareSettings.CustomControl.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.HardwareSettings.HostBuilders
{
    public static class AddPagesHostBuilderExtensions
    {
        public static IHostBuilder AddPages(this IHostBuilder host)
        {
            return host.ConfigureServices((HostBuilderContext context, IServiceCollection services) =>
            {
                services.AddTransient<LanfengView>();
                services.AddTransient<LanfengPage>();

                services.AddKeyedTransient<IPage, LanfengPage>(PageType.Lanfeng);

                services.AddSingleton<IPageFactory, PageFactory>();
            });
        }
    }
}

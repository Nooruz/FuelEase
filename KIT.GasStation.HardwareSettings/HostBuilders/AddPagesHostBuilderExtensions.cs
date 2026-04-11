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
                // Views
                services.AddTransient<LanfengView>();
                services.AddTransient<GilbarcoView>();
                services.AddTransient<PKElectronicsView>();
                services.AddTransient<EmulatorView>();
                services.AddTransient<EKassaView>();
                services.AddTransient<NewCasView>();
                services.AddTransient<KITView>();

                // Pages
                services.AddTransient<LanfengPage>();
                services.AddTransient<GilbarcoPage>();
                services.AddTransient<PKElectronicsPage>();
                services.AddTransient<EmulatorPage>();
                services.AddTransient<EKassaPage>();
                services.AddTransient<NewCasPage>();
                services.AddTransient<KITPage>();

                // Keyed services for PageFactory
                services.AddKeyedTransient<IPage, LanfengPage>(PageType.Lanfeng);
                services.AddKeyedTransient<IPage, GilbarcoPage>(PageType.Gilbarco);
                services.AddKeyedTransient<IPage, PKElectronicsPage>(PageType.PKElectronics);
                services.AddKeyedTransient<IPage, EmulatorPage>(PageType.Emulator);
                services.AddKeyedTransient<IPage, EKassaPage>(PageType.EKassa);
                services.AddKeyedTransient<IPage, NewCasPage>(PageType.NewCas);
                services.AddKeyedTransient<IPage, KITPage>(PageType.KIT);

                // Factory
                services.AddSingleton<IPageFactory, PageFactory>();
            });
        }
    }
}

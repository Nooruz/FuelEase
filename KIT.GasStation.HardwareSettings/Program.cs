using KIT.App.Infrastructure.HostBuilders;
using KIT.GasStation.HardwareSettings.HostBuilders;
using KIT.GasStation.HardwareSettings.Presenters;
using KIT.GasStation.HardwareSettings.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.HardwareSettings
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            var host = CreateHostBuilder().Build();

            // Просим DI собрать MainForm (и всё, что ему нужно)
            var mainForm = host.Services.GetRequiredService<Main>();

            // presenter получает именно этот экземпляр формы как IMainView
            ActivatorUtilities.CreateInstance<MainPresenter>(host.Services, (IMainView)mainForm);
            Application.Run(mainForm);
        }

        static IHostBuilder CreateHostBuilder(string[]? args = null)
        {
            args ??= Array.Empty<string>();

            return Host.CreateDefaultBuilder(args)
                .AddLocalServices()
                .AddHardwareConfigurationsServices()
                .AddPages()
                .AddPresenters()
                .AddViews();
        }
    }
}
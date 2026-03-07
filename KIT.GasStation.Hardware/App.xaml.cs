using KIT.App.Infrastructure.HostBuilders;
using KIT.GasStation.Hardware.HostBuilders;
using KIT.GasStation.Hardware.ViewModels;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Reflection;
using System.Windows;

namespace KIT.GasStation.Hardware
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region Private Members

        private readonly IHost _host;

        #endregion

        #region Static Properties

        public static string CompanyName
        {
            get
            {
                // Пример получения из атрибута AssemblyCompany
                var attribute = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyCompanyAttribute>();
                return attribute?.Company ?? "ОсОО \"КИТ\"";
            }
        }
        public static string ProductName
        {
            get
            {
                // Пример получения из атрибута AssemblyCompany
                var attribute = Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyProductAttribute>();
                return attribute?.Product ?? "КИТ-АЗС Конфигуратор";
            }
        }

        #endregion

        #region Constructor

        public App()
        {
            _host = CreateHostBuilder().Build();
        }

        #endregion

        private static IHostBuilder CreateHostBuilder(string[] args = null)
        {
            return Host.CreateDefaultBuilder(args)
                .AddHardwareConfigurationsServices()
                .AddCashRegisters()
                .AddStores()
                .AddViewModels();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                await _host.StartAsync();

                var hardwareConfigurator = _host.Services.GetRequiredService<IHardwareConfigurationService>();

                await hardwareConfigurator.EnsureConfigurationFileExistsAsync();

                Window window = _host.Services.GetRequiredService<MainWindow>();
                window.DataContext = _host.Services.GetRequiredService<MainWindowViewModel>();

                window.Show();
            }
            catch (Exception exc)
            {
                MessageBox.Show($"{exc.Message} | {exc.Source} | {exc.StackTrace}");
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host.StopAsync();
            _host.Dispose();

            base.OnExit(e);
        }
    }

}

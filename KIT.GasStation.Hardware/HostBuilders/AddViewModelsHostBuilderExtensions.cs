using KIT.GasStation.Common.Factories;
using KIT.GasStation.Hardware.State.Navigators;
using KIT.GasStation.Hardware.ViewModels;
using KIT.GasStation.Hardware.ViewModels.Factories;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace KIT.GasStation.Hardware.HostBuilders
{
    public static class AddViewModelsHostBuilderExtensions
    {
        public static IHostBuilder AddViewModels(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                services.AddSingleton(s => new MainWindow());

                services.AddTransient(CreateMainWindowViewModel);
                services.AddTransient(CreateLanfengViewModel);
                services.AddTransient(CreatePKElectronicsViewModel);
                services.AddTransient(CreateEKassaViewModel);
                services.AddTransient(CreateNewCasViewModel);
                services.AddTransient(CreateKITViewModel);
                services.AddTransient(CreateGilbarcoViewModel);
                services.AddTransient(CreateEmulatorViewModel);

                services.AddSingleton<CreateViewModel<KITViewModel>>(servicesProvider => () => CreateKITViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<MainWindowViewModel>>(servicesProvider => () => CreateMainWindowViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<LanfengViewModel>>(servicesProvider => () => CreateLanfengViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<PKElectronicsViewModel>>(servicesProvider => () => CreatePKElectronicsViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<EKassaViewModel>>(servicesProvider => () => CreateEKassaViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<NewCasViewModel>>(servicesProvider => () => CreateNewCasViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<GilbarcoViewModel>>(servicesProvider => () => CreateGilbarcoViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<EmulatorViewModel>>(servicesProvider => () => CreateEmulatorViewModel(servicesProvider));

                services.AddSingleton<IViewModelFactory, ViewModelFactory>();
            });
        }

        private static EmulatorViewModel CreateEmulatorViewModel(IServiceProvider services)
        {
            return new EmulatorViewModel(services.GetRequiredService<IHardwareConfigurationService>());
        }

        private static KITViewModel CreateKITViewModel(IServiceProvider services)
        {
            return new KITViewModel();
        }

        private static NewCasViewModel CreateNewCasViewModel(IServiceProvider services)
        {
            return new NewCasViewModel(services.GetRequiredService<IHardwareConfigurationService>(),
                services.GetRequiredService<ICashRegisterFactory>());
        }

        private static EKassaViewModel CreateEKassaViewModel(IServiceProvider services)
        {
            return new EKassaViewModel(services.GetRequiredService<IHardwareConfigurationService>(),
                services.GetRequiredService<ICashRegisterFactory>());
        }

        private static MainWindowViewModel CreateMainWindowViewModel(IServiceProvider services)
        {
            return new MainWindowViewModel(services.GetRequiredService<INavigator>(),
                services.GetRequiredService<IHardwareConfigurationService>());
        }

        private static GilbarcoViewModel CreateGilbarcoViewModel(IServiceProvider services)
        {
            return new GilbarcoViewModel(services.GetRequiredService<IHardwareConfigurationService>());
        }

        private static LanfengViewModel CreateLanfengViewModel(IServiceProvider services)
        {
            return new LanfengViewModel(services.GetRequiredService<IHardwareConfigurationService>(),
                services.GetRequiredService<IFuelDispenserFactory>());
        }

        private static PKElectronicsViewModel CreatePKElectronicsViewModel(IServiceProvider services)
        {
            return new PKElectronicsViewModel(services.GetRequiredService<IHardwareConfigurationService>(),
                services.GetRequiredService<IFuelDispenserFactory>());
        }
    }
}

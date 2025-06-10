using FuelEase.Common.Factories;
using FuelEase.Domain.Models;
using FuelEase.FuelDispenser.Services;
using FuelEase.FuelDispenser.Services.Factories;
using FuelEase.FuelDispenserEmulator;
using FuelEase.HardwareConfigurations.Services;
using FuelEase.Lanfeng;
using FuelEase.PKElectronics;
using FuelEase.TechnoProjekt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FuelEase.Common.HostBuilders
{
    /// <summary>
    /// Расширения для добавления сервисов работы с топливными терминалами
    /// </summary>
    public static class AddFuelDispensersHostBuilderExtensions
    {
        /// <summary>
        /// Добавление сервисов работы с топливными терминалами
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static IHostBuilder AddFuelDispensers(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                //services.AddTransient(CreateLanfengFuelDispenser);
                //services.AddTransient(CreatePKElectronicsFuelDispenser);
                services.AddTransient(CreateTechnoProjektFuelDispenser);
                services.AddTransient(CreateEmulatorFuelDispenser);

                services.AddSingleton<CreateFuelDispenser<LanfengFuelDispenser>>(servicesProvider => (nozzles) => CreateLanfengFuelDispenser(servicesProvider, nozzles));
                services.AddSingleton<CreateFuelDispenser<PKElectronicsFuelDispenser>>(servicesProvider => (nozzles) => CreatePKElectronicsFuelDispenser(servicesProvider, nozzles));
                //services.AddSingleton<CreateFuelDispenser<TechnoProjektFuelDispenser>>(servicesProvider => () => CreateTechnoProjektFuelDispenser(servicesProvider));
                //services.AddSingleton<CreateFuelDispenser<EmulatorFuelDispenser>>(servicesProvider => () => CreateEmulatorFuelDispenser(servicesProvider));

                services.AddSingleton<IFuelDispenserFactory, FuelDispenserFactory>();
            });
        }

        /// <summary>
        /// Создание экземпляра сервиса для работы с топливным терминалом Lanfeng
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static LanfengFuelDispenser CreateLanfengFuelDispenser(IServiceProvider services, 
            IEnumerable<Nozzle>? nozzles)
        {
            return new LanfengFuelDispenser(services.GetRequiredService<IPortManager>(),
                services.GetRequiredService<IHardwareConfigurationService>(),
                services.GetRequiredService<IProtocolParserFactory>(),
                nozzles);
        }

        /// <summary>
        /// Создание экземпляра сервиса для работы с топливным терминалом PKElectronics
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static PKElectronicsFuelDispenser CreatePKElectronicsFuelDispenser(IServiceProvider services,
            IEnumerable<Nozzle>? nozzles)
        {
            return new PKElectronicsFuelDispenser(services.GetRequiredService<IPortManager>(),
                services.GetRequiredService<IHardwareConfigurationService>(),
                services.GetRequiredService<IProtocolParserFactory>(),
                nozzles);
        }

        /// <summary>
        /// Создание экземпляра сервиса для работы с топливным терминалом TechnoProjekt
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static TechnoProjektFuelDispenser CreateTechnoProjektFuelDispenser(IServiceProvider services)
        {
            return new TechnoProjektFuelDispenser();
        }

        /// <summary>
        /// Создание экземпляра эмулятора топливного терминала
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static EmulatorFuelDispenser CreateEmulatorFuelDispenser(IServiceProvider services)
        {
            return new EmulatorFuelDispenser();
        }
    }
}

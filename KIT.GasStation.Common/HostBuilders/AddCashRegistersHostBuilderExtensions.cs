using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.Common.Factories;
using KIT.GasStation.EKassa;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.NewCas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.Common.HostBuilders
{
    /// <summary>
    /// Расширения для добавления сервисов работы с кассами
    /// </summary>
    public static class AddCashRegistersHostBuilderExtensions
    {
        /// <summary>
        /// Добавление сервисов работы с топливными терминалами
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static IHostBuilder AddCashRegisters(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                services.AddTransient(CreateEKassaCashRegister);
                services.AddTransient(CreateNewCasCashRegister);

                services.AddSingleton<CreateCashRegister<EKassaCashRegister>>(servicesProvider => () => CreateEKassaCashRegister(servicesProvider));
                services.AddSingleton<CreateCashRegister<NewCasCashRegister>>(servicesProvider => () => CreateNewCasCashRegister(servicesProvider));

                services.AddSingleton<ICashRegisterFactory, CashRegisterFactory>();
            });
        }

        /// <summary>
        /// Создание экземпляра сервиса для работы с кассой ЕКасса
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static EKassaCashRegister CreateEKassaCashRegister(IServiceProvider services)
        {
            return new EKassaCashRegister(services.GetRequiredService<IHardwareConfigurationService>());
        }

        public static NewCasCashRegister CreateNewCasCashRegister(IServiceProvider services)
        {
            return new NewCasCashRegister(services.GetRequiredService<IHardwareConfigurationService>());
        }
    }
}

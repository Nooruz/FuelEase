using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Services.AuthenticationServices;
using KIT.GasStation.Domain.Views;
using KIT.GasStation.EntityFramework.Services;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.SplashScreen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.GasStation.HostBuilders
{
    public static class AddServicesHostBuilderExtensions
    {
        public static IHostBuilder AddServices(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                _ = services.AddSingleton<IAuthenticationService, AuthenticationService>();
                _ = services.AddSingleton<IUserService, UserService>();
                _ = services.AddSingleton<IShiftService, ShiftService>();
                _ = services.AddSingleton<IFuelSaleService, FuelSaleService>();
                _ = services.AddSingleton<IFuelSaleService, FuelSaleService>();
                _ = services.AddSingleton<IFuelService, FuelService>();
                _ = services.AddSingleton<IUnitOfMeasurementService, UnitOfMeasurementService>();
                _ = services.AddSingleton<ITankService, TankService>();
                _ = services.AddSingleton<IViewService<TankFuelQuantityView>, ViewService<TankFuelQuantityView>>();
                _ = services.AddSingleton<IViewService<NozzleMeterValueView>, ViewService<NozzleMeterValueView>>();
                _ = services.AddSingleton<IDataService<FuelRevaluation>, GenericService<FuelRevaluation>>();
                _ = services.AddSingleton<IUnregisteredSaleService, UnregisteredSaleService>();
                _ = services.AddSingleton<IDataService<UserRole>, GenericService<UserRole>>();
                _ = services.AddSingleton<IShiftCounterService, ShiftCounterService>();
                _ = services.AddSingleton<ITankShiftCounterService, TankShiftCounterService>();
                _ = services.AddSingleton<IEventPanelService, EventPanelService>();
                _ = services.AddSingleton<ICustomSplashScreenService, CustomSplashScreenService>();
                _ = services.AddSingleton<INozzleService, NozzleService>();
                _ = services.AddSingleton<IDiscountService, DiscountService>();
                _ = services.AddSingleton<IFiscalDataService, FiscalDataService>();
                _ = services.AddSingleton<IHardwareConfigurationService, HardwareConfigurationService>();
                _ = services.AddSingleton<IFuelIntakeService, FuelIntakeService>();
            });
        }
    }
}

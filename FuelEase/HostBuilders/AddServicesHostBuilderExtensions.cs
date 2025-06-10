using FuelEase.Domain.Models;
using FuelEase.Domain.Services;
using FuelEase.Domain.Services.AuthenticationServices;
using FuelEase.Domain.Views;
using FuelEase.EntityFramework.Services;
using FuelEase.HardwareConfigurations.Services;
using FuelEase.SplashScreen;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace FuelEase.HostBuilders
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

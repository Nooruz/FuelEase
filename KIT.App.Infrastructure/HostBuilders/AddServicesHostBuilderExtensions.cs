using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Services.AuthenticationServices;
using KIT.GasStation.Domain.Views;
using KIT.GasStation.EntityFramework.Services;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace KIT.App.Infrastructure.HostBuilders
{
    public static class AddServicesHostBuilderExtensions
    {
        public static IHostBuilder AddServices(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                services.AddSingleton<IAuthenticationService, AuthenticationService>();
                services.AddSingleton<IUserService, UserService>();
                services.AddSingleton<IShiftService, ShiftService>();
                services.AddSingleton<IFuelSaleService, FuelSaleService>();
                services.AddSingleton<IFuelSaleService, FuelSaleService>();
                services.AddSingleton<IFuelService, FuelService>();
                services.AddSingleton<IUnitOfMeasurementService, UnitOfMeasurementService>();
                services.AddSingleton<ITankService, TankService>();
                services.AddSingleton<IViewService<TankFuelQuantityView>, ViewService<TankFuelQuantityView>>();
                services.AddSingleton<IViewService<NozzleMeterValueView>, ViewService<NozzleMeterValueView>>();
                services.AddSingleton<IDataService<FuelRevaluation>, GenericService<FuelRevaluation>>();
                services.AddSingleton<IUnregisteredSaleService, UnregisteredSaleService>();
                services.AddSingleton<IDataService<UserRole>, GenericService<UserRole>>();
                services.AddSingleton<IShiftCounterService, ShiftCounterService>();
                services.AddSingleton<ITankShiftCounterService, TankShiftCounterService>();
                services.AddSingleton<IEventPanelService, EventPanelService>();
                services.AddSingleton<INozzleService, NozzleService>();
                services.AddSingleton<IDiscountService, DiscountService>();
                services.AddSingleton<IFiscalDataService, FiscalDataService>();
                services.AddSingleton<IFuelIntakeService, FuelIntakeService>();
                services.AddSingleton<IHardwareConfigurationService, HardwareConfigurationService>();
            });
        }
    }
}

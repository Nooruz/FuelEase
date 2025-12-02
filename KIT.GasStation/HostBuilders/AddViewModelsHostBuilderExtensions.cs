using DevExpress.Mvvm.POCO;
using KIT.GasStation.Domain.Models;
using KIT.GasStation.Domain.Services;
using KIT.GasStation.Domain.Views;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.SplashScreen;
using KIT.GasStation.State.Authenticators;
using KIT.GasStation.State.CashRegisters;
using KIT.GasStation.State.Discounts;
using KIT.GasStation.State.Navigators;
using KIT.GasStation.State.Notifications;
using KIT.GasStation.State.Nozzles;
using KIT.GasStation.State.Shifts;
using KIT.GasStation.State.Users;
using KIT.GasStation.ViewModels;
using KIT.GasStation.ViewModels.Base;
using KIT.GasStation.ViewModels.Details;
using KIT.GasStation.ViewModels.Discounts;
using KIT.GasStation.ViewModels.Factories;
using KIT.GasStation.ViewModels.GlobalReports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace KIT.GasStation.HostBuilders
{
    public static class AddViewModelsHostBuilderExtensions
    {
        public static IHostBuilder AddViewModels(this IHostBuilder host)
        {
            return host.ConfigureServices(services =>
            {
                services.AddSingleton(s => new MainWindow());

                services.AddTransient(CreateRevaluationViewModel);
                services.AddTransient(CreateUncompletedSalesViewModel);
                services.AddTransient(CreateCompletedSalesViewModel);
                services.AddTransient(CreateFuelIntakeDetailViewModel);
                services.AddTransient(CreateTanksViewModel);
                services.AddTransient(CreateCashViewModel);
                services.AddTransient(CreateConfigurationManagementViewModel);
                services.AddTransient(CreateNozzleCounterPanelViewModel);
                services.AddTransient(CreateTanksPanelViewModel);
                services.AddTransient(CreateControllerListViewModel);
                services.AddTransient(CreateMainViewModel);
                services.AddTransient(CreateFuelSaleViewModel);
                services.AddTransient(CreateEventPanelViewModel);
                services.AddSingleton(CreateMainWindowViewModel);
                services.AddSingleton(CreateLoginViewModel);
                services.AddTransient(CreateUnregisteredSalePanelViewModel);
                services.AddTransient(CreateSaleManagerViewModel);
                services.AddTransient(CreateCustomReceiptViewModel);
                services.AddTransient(CreateGlobalReportViewModel);
                services.AddTransient(CreateUserViewModel);
                services.AddTransient(CreateDiscountManagementViewModel);
                services.AddTransient(CreateDiscountViewModel);
                services.AddTransient(CreateFuelDispenserViewModel);
                services.AddTransient(CreateWorkplaceSettingsViewModel);

                

                services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<MainWindowViewModel>());
                services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<LoginViewModel>());

                services.AddSingleton<CreateViewModel<RevaluationViewModel>>(servicesProvider => () => CreateRevaluationViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<UncompletedSalesViewModel>>(servicesProvider => () => CreateUncompletedSalesViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<CompletedSalesViewModel>>(servicesProvider => () => CreateCompletedSalesViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<FuelIntakeDetailViewModel>>(servicesProvider => () => CreateFuelIntakeDetailViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<TanksViewModel>>(servicesProvider => () => CreateTanksViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<CashViewModel>>(servicesProvider => () => CreateCashViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<ConfigurationManagementViewModel>>(servicesProvider => () => CreateConfigurationManagementViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<NozzleCounterPanelViewModel>>(servicesProvider => () => CreateNozzleCounterPanelViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<TanksPanelViewModel>>(servicesProvider => () => CreateTanksPanelViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<UnregisteredSalePanelViewModel>>(servicesProvider => () => CreateUnregisteredSalePanelViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<SaleManagerViewModel>>(servicesProvider => () => CreateSaleManagerViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<CustomReceiptViewModel>>(servicesProvider => () => CreateCustomReceiptViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<GlobalReportViewModel>>(servicesProvider => () => CreateGlobalReportViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<UserViewModel>>(servicesProvider => () => CreateUserViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<DiscountManagementViewModel>>(servicesProvider => () => CreateDiscountManagementViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<DiscountViewModel>>(servicesProvider => () => CreateDiscountViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<FuelDispenserViewModel>>(servicesProvider => () => CreateFuelDispenserViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<WorkplaceSettingsViewModel>>(servicesProvider => () => CreateWorkplaceSettingsViewModel(servicesProvider));
                
                services.AddSingleton<CreateViewModel<ControllerListViewModel>>(servicesProvider => () => CreateControllerListViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<MainViewModel>>(servicesProvider => () => CreateMainViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<FuelSaleViewModel>>(servicesProvider => () => CreateFuelSaleViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<EventPanelViewModel>>(servicesProvider => () => CreateEventPanelViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<MainWindowViewModel>>(servicesProvider => () => CreateMainWindowViewModel(servicesProvider));
                services.AddSingleton<CreateViewModel<LoginViewModel>>(servicesProvider => () => CreateLoginViewModel(servicesProvider));

                services.AddSingleton<IViewModelFactory, ViewModelFactory>();

                services.AddSingleton<ViewModelDelegateRenavigator<MainViewModel>>();
                services.AddSingleton<ViewModelDelegateRenavigator<LoginViewModel>>();
            });
        }

        private static WorkplaceSettingsViewModel CreateWorkplaceSettingsViewModel(IServiceProvider services)
        {
            return new WorkplaceSettingsViewModel(services.GetRequiredService<ICashRegisterStore>());
        }

        private static FuelDispenserViewModel CreateFuelDispenserViewModel(IServiceProvider services)
        {
            return new FuelDispenserViewModel(services.GetRequiredService<INozzleStore>(),
                services.GetRequiredService<IFuelSaleService>(),
                services.GetRequiredService<ICashRegisterStore>(),
                services.GetRequiredService<IShiftStore>(),
                services.GetRequiredService<IShiftCounterService>(),
                services.GetRequiredService<IUnregisteredSaleService>(),
                services.GetRequiredService<IFuelService>(),
                services.GetRequiredService<IUserStore>(),
                services.GetRequiredService<IHubClient>(),
                services.GetRequiredService<IViewService<TankFuelQuantityView>>());
        }

        private static DiscountViewModel CreateDiscountViewModel(IServiceProvider services)
        {
            return new DiscountViewModel(services.GetRequiredService<ILogger<DiscountViewModel>>(),
                services.GetRequiredService<IDiscountService>(),
                services.GetRequiredService<IFuelService>());
        }

        private static DiscountManagementViewModel CreateDiscountManagementViewModel(IServiceProvider services)
        {
            return new DiscountManagementViewModel(services.GetRequiredService<INavigator>());
        }

        private static UserViewModel CreateUserViewModel(IServiceProvider services)
        {
            return new UserViewModel(services.GetRequiredService<ILogger<UserViewModel>>(),
                services.GetRequiredService<IUserService>(),
                services.GetRequiredService<IDataService<UserRole>>(),
                services.GetRequiredService<ILogger<UserDetailViewModel>>());
        }

        private static GlobalReportViewModel CreateGlobalReportViewModel(IServiceProvider services)
        {
            return new GlobalReportViewModel(services.GetRequiredService<ILogger<GlobalReportViewModel>>(),
                services.GetRequiredService<INavigator>(),
                services.GetRequiredService<ILogger<ShiftInfoViewModel>>(),
                services.GetRequiredService<IShiftService>(),
                services.GetRequiredService<IUserStore>(),
                services.GetRequiredService<IFuelSaleService>(),
                services.GetRequiredService<IShiftStore>(),
                services.GetRequiredService<IFuelService>(),
                services.GetRequiredService<INozzleService>(),
                services.GetRequiredService<IShiftCounterService>(),
                services.GetRequiredService<ITankService>(),
                services.GetRequiredService<IFuelIntakeService>(),
                services.GetRequiredService<ITankShiftCounterService>(),
                services.GetRequiredService<INozzleStore>(),
                services.GetRequiredService<IViewService<TankFuelQuantityView>>());
        }

        private static CustomReceiptViewModel CreateCustomReceiptViewModel(IServiceProvider services)
        {
            return new CustomReceiptViewModel(services.GetRequiredService<ILogger<CustomReceiptViewModel>>(),
                services.GetRequiredService<IFuelService>());
        }

        private static SaleManagerViewModel CreateSaleManagerViewModel(IServiceProvider services)
        {
            return new SaleManagerViewModel(services.GetRequiredService<ILogger<SaleManagerViewModel>>(),
                services.GetRequiredService<IFuelSaleService>());
        }

        private static UnregisteredSalePanelViewModel CreateUnregisteredSalePanelViewModel(IServiceProvider services)
        {
            return new UnregisteredSalePanelViewModel(services.GetRequiredService<ILogger<UnregisteredSalePanelViewModel>>(),
                services.GetRequiredService<ILogger<SaleManagerViewModel>>(),
                services.GetRequiredService<IFuelSaleService>(),
                services.GetRequiredService<IUnregisteredSaleService>(),
                services.GetRequiredService<IShiftStore>(),
                services.GetRequiredService<INavigator>(),
                services.GetRequiredService<ICustomSplashScreenService>(),
                services.GetRequiredService<IDisñountStore>(),
                services.GetRequiredService<ICashRegisterStore>(),
                services.GetRequiredService<IFuelService>());
        }

        private static RevaluationViewModel CreateRevaluationViewModel(IServiceProvider services)
        {
            return new RevaluationViewModel(services.GetRequiredService<IFuelService>(),
                services.GetRequiredService<IDataService<FuelRevaluation>>());
        }

        private static UncompletedSalesViewModel CreateUncompletedSalesViewModel(IServiceProvider services)
        {
            return new UncompletedSalesViewModel(services.GetRequiredService<ILogger<UncompletedSalesViewModel>>(),
                services.GetRequiredService<IShiftStore>(),
                services.GetRequiredService<IFuelSaleService>(),
                services.GetRequiredService<ICashRegisterStore>(),
                services.GetRequiredService<IUserStore>());
        }

        private static CompletedSalesViewModel CreateCompletedSalesViewModel(IServiceProvider services)
        {
            return new CompletedSalesViewModel(services.GetRequiredService<IShiftStore>(),
                services.GetRequiredService<IFuelSaleService>(),
                services.GetRequiredService<ICashRegisterStore>(),
                services.GetRequiredService<IFiscalDataService>());
        }

        private static FuelIntakeDetailViewModel CreateFuelIntakeDetailViewModel(IServiceProvider services)
        {
            return new FuelIntakeDetailViewModel(services.GetRequiredService<IFuelIntakeService>(),
                services.GetRequiredService<IViewService<TankFuelQuantityView>>(),
                services.GetRequiredService<ITankService>(),
                services.GetRequiredService<IShiftStore>());
        }

        private static TanksPanelViewModel CreateTanksPanelViewModel(IServiceProvider services)
        {
            return new TanksPanelViewModel(services.GetRequiredService<ILogger<TanksPanelViewModel>>(),
                services.GetRequiredService<IViewService<TankFuelQuantityView>>(),
                services.GetRequiredService<ITankService>(),
                services.GetRequiredService<IFuelService>(),
                services.GetRequiredService<IFuelIntakeService>(),
                services.GetRequiredService<IFuelSaleService>(),
                services.GetRequiredService<ITankShiftCounterService>(),
                services.GetRequiredService<IShiftStore>());
        }

        private static NozzleCounterPanelViewModel CreateNozzleCounterPanelViewModel(IServiceProvider services)
        {
            return new NozzleCounterPanelViewModel(services.GetRequiredService<INozzleStore>(),
                services.GetRequiredService<ILogger<NozzleCounterPanelViewModel>>());
        }

        private static ConfigurationManagementViewModel CreateConfigurationManagementViewModel(IServiceProvider services)
        {
            return new ConfigurationManagementViewModel(services.GetRequiredService<INavigator>());
        }

        private static CashViewModel CreateCashViewModel(IServiceProvider services)
        {
            return new CashViewModel(services.GetRequiredService<IShiftStore>(),
                services.GetRequiredService<IFuelSaleService>());
        }

        private static TanksViewModel CreateTanksViewModel(IServiceProvider services)
        {
            return new TanksViewModel(services.GetRequiredService<IFuelService>(),
                services.GetRequiredService<INozzleService>(),
                services.GetRequiredService<IUnitOfMeasurementService>(),
                services.GetRequiredService<ITankService>(),
                services.GetRequiredService<IViewService<TankFuelQuantityView>>(),
                services.GetRequiredService<ILogger<TanksViewModel>>(),
                services.GetRequiredService<IHubClient>());
        }

        private static EventPanelViewModel CreateEventPanelViewModel(IServiceProvider services)
        {
            return new EventPanelViewModel(services.GetRequiredService<ILogger<EventPanelViewModel>>(),
                services.GetRequiredService<IEventPanelService>(),
                services.GetRequiredService<IShiftStore>(),
                services.GetRequiredService<IFuelSaleService>());
        }

        private static FuelSaleViewModel CreateFuelSaleViewModel(IServiceProvider services)
        {
            return new FuelSaleViewModel(services.GetRequiredService<INozzleStore>(),
                services.GetRequiredService<IFuelSaleService>(),
                services.GetRequiredService<IViewService<TankFuelQuantityView>>(),
                services.GetRequiredService<ILogger<FuelSaleViewModel>>(),
                services.GetRequiredService<IFuelService>(),
                services.GetRequiredService<IShiftStore>(),
                services.GetRequiredService<ICustomSplashScreenService>(),
                services.GetRequiredService<IDisñountStore>(),
                services.GetRequiredService<ICashRegisterStore>());
        }

        private static LoginViewModel CreateLoginViewModel(IServiceProvider services)
        {
            return new LoginViewModel(services.GetRequiredService<ILogger<LoginViewModel>>(),
                services.GetRequiredService<IAuthenticator>(),
                services.GetRequiredService<IUserService>(),
                services.GetRequiredService<IShiftService>());
        }

        private static ControllerListViewModel CreateControllerListViewModel(IServiceProvider services)
        {
            return new ControllerListViewModel(services.GetRequiredService<INozzleStore>(),
                services.GetRequiredService<INavigator>(),
                services.GetRequiredService<IViewService<TankFuelQuantityView>>(),
                services.GetRequiredService<ILogger<ControllerListViewModel>>());
        }

        private static MainViewModel CreateMainViewModel(IServiceProvider services)
        {
            return new MainViewModel(services.GetRequiredService<INavigator>(),
                services.GetRequiredService<ILogger<MainViewModel>>(),
                services.GetRequiredService<IShiftStore>(),
                services.GetRequiredService<IUserStore>(),
                services.GetRequiredService<ViewModelDelegateRenavigator<LoginViewModel>>(),
                services.GetRequiredService<IFuelSaleService>(),
                services.GetRequiredService<IUnregisteredSaleService>(),
                services.GetRequiredService<IAuthenticator>(),
                services.GetRequiredService<ICustomSplashScreenService>(),
                services.GetRequiredService<ICashRegisterStore>(),
                services.GetRequiredService<INotificationStore>());
        }

        private static MainWindowViewModel CreateMainWindowViewModel(IServiceProvider services)
        {
            return new MainWindowViewModel(services.GetRequiredService<INavigator>(),
                services.GetRequiredService<ILogger<MainWindowViewModel>>(),
                services.GetRequiredService<ICustomSplashScreenService>());
        }
    }
}


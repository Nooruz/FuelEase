using FuelEase.State.Navigators;
using FuelEase.ViewModels.Base;
using FuelEase.ViewModels.Details;
using FuelEase.ViewModels.Discounts;
using System;
using System.Threading.Tasks;

namespace FuelEase.ViewModels.Factories
{
    public class ViewModelFactory : IViewModelFactory
    {
        #region Private Members

        private readonly CreateViewModel<MainViewModel> _createMainViewModel;
        private readonly CreateViewModel<LoginViewModel> _createLoginViewModel;
        private readonly CreateViewModel<MainWindowViewModel> _createMainWindowViewModel;
        private readonly CreateViewModel<CashViewModel> _createCashViewModel;
        private readonly CreateViewModel<ConfigurationManagementViewModel> _createConfigurationManagementViewModel;
        private readonly CreateViewModel<EventPanelViewModel> _createEventPanelViewModel;
        private readonly CreateViewModel<NozzleCounterPanelViewModel> _createNozzleCounterPanelViewModel;
        private readonly CreateViewModel<FuelSaleViewModel> _createFuelSaleViewModel;
        private readonly CreateViewModel<TanksViewModel> _createTanksViewModel;
        private readonly CreateViewModel<TanksPanelViewModel> _createTanksPanelViewModel;
        private readonly CreateViewModel<FuelIntakeDetailViewModel> _createFuelIntakeDetailViewModel;
        private readonly CreateViewModel<UncompletedSalesViewModel> _createUncompletedSalesViewModel;
        private readonly CreateViewModel<CompletedSalesViewModel> _createCompletedSalesViewModel;
        private readonly CreateViewModel<RevaluationViewModel> _createRevaluationViewModel;
        private readonly CreateViewModel<ControllerListViewModel> _createControllerListViewModel;
        private readonly CreateViewModel<UnregisteredSalePanelViewModel> _createUnregisteredSalePanelViewModel;
        private readonly CreateViewModel<SaleManagerViewModel> _createSaleManagerViewModel;
        private readonly CreateViewModel<CustomReceiptViewModel> _createCustomReceiptViewModel;
        private readonly CreateViewModel<GlobalReportViewModel> _createGlobalReportViewModel;
        private readonly CreateViewModel<UserViewModel> _createUserViewModel;
        private readonly CreateViewModel<DiscountManagementViewModel> _createDiscountManagementViewModel;
        private readonly CreateViewModel<DiscountViewModel> _createDiscountViewModel;
        private readonly CreateViewModel<FuelDispenserViewModel> _createFuelDispenserViewModel;
        private readonly CreateViewModel<WorkplaceSettingsViewModel> _createWorkplaceSettingsViewModel;

        #endregion

        #region Constructor

        public ViewModelFactory(CreateViewModel<MainViewModel> createMainViewModel, 
            CreateViewModel<LoginViewModel> createLoginViewModel,
            CreateViewModel<MainWindowViewModel> createMainWindowViewModel,
            CreateViewModel<CashViewModel> createCashViewModel,
            CreateViewModel<ConfigurationManagementViewModel> createConfigurationManagementViewModel,
            CreateViewModel<EventPanelViewModel> createEventPanelViewModel,
            CreateViewModel<NozzleCounterPanelViewModel> createNozzleCounterPanelViewModel,
            CreateViewModel<FuelSaleViewModel> createFuelSaleViewModel,
            CreateViewModel<TanksViewModel> createTanksViewModel,
            CreateViewModel<TanksPanelViewModel> createTanksPanelViewModel,
            CreateViewModel<FuelIntakeDetailViewModel> createFuelIntakeDetailViewModel,
            CreateViewModel<UncompletedSalesViewModel> createUncompletedSalesViewModel,
            CreateViewModel<RevaluationViewModel> createRevaluationViewModel,
            CreateViewModel<ControllerListViewModel> createControllerListViewModel,
            CreateViewModel<UnregisteredSalePanelViewModel> createUnregisteredSalePanelViewModel,
            CreateViewModel<SaleManagerViewModel> createSaleManagerViewModel,
            CreateViewModel<CompletedSalesViewModel> createCompletedSalesViewModel,
            CreateViewModel<CustomReceiptViewModel> createCustomReceiptViewModel,
            CreateViewModel<GlobalReportViewModel> createGlobalReportViewModel,
            CreateViewModel<UserViewModel> createUserViewModel,
            CreateViewModel<DiscountManagementViewModel> createDiscountManagementViewModel,
            CreateViewModel<DiscountViewModel> createDiscountViewModel,
            CreateViewModel<FuelDispenserViewModel> createFuelDispenserViewModel,
            CreateViewModel<WorkplaceSettingsViewModel> createWorkplaceSettingsViewModel)
        {
            _createMainViewModel = createMainViewModel;
            _createLoginViewModel = createLoginViewModel;
            _createMainWindowViewModel = createMainWindowViewModel;
            _createCashViewModel = createCashViewModel;
            _createConfigurationManagementViewModel = createConfigurationManagementViewModel;
            _createEventPanelViewModel = createEventPanelViewModel;
            _createNozzleCounterPanelViewModel = createNozzleCounterPanelViewModel;
            _createFuelSaleViewModel = createFuelSaleViewModel;
            _createTanksViewModel = createTanksViewModel;
            _createTanksPanelViewModel = createTanksPanelViewModel;
            _createFuelIntakeDetailViewModel = createFuelIntakeDetailViewModel;
            _createUncompletedSalesViewModel = createUncompletedSalesViewModel;
            _createRevaluationViewModel = createRevaluationViewModel;
            _createControllerListViewModel = createControllerListViewModel;
            _createUnregisteredSalePanelViewModel = createUnregisteredSalePanelViewModel;
            _createSaleManagerViewModel = createSaleManagerViewModel;
            _createCompletedSalesViewModel = createCompletedSalesViewModel;
            _createCustomReceiptViewModel = createCustomReceiptViewModel;
            _createGlobalReportViewModel = createGlobalReportViewModel;
            _createUserViewModel = createUserViewModel;
            _createDiscountManagementViewModel = createDiscountManagementViewModel;
            _createDiscountViewModel = createDiscountViewModel;
            _createFuelDispenserViewModel = createFuelDispenserViewModel;
            _createWorkplaceSettingsViewModel = createWorkplaceSettingsViewModel;
        }

        #endregion

        #region Public Voids

        public async Task<BaseViewModel> CreateViewModelAsync(ViewType viewType)
        {
            // Выполнение создания ViewModel в асинхронном методе (например, имитация задержки для асинхронной инициализации)
            return await Task.Run(() =>
            {
                return viewType switch
                {
                    ViewType.Main => (BaseViewModel)_createMainViewModel(),
                    ViewType.Login => _createLoginViewModel(),
                    ViewType.Home => _createMainViewModel(),
                    ViewType.MainWindow => _createMainWindowViewModel(),
                    ViewType.CashView => _createCashViewModel(),
                    ViewType.ConfigurationManagementView => _createConfigurationManagementViewModel(),
                    ViewType.EventPanelView => _createEventPanelViewModel(),
                    ViewType.FuelSaleView => _createFuelSaleViewModel(),
                    ViewType.TanksPanelView => _createTanksPanelViewModel(),
                    ViewType.TanksView => _createTanksViewModel(),
                    ViewType.FuelIntakeDetailView => _createFuelIntakeDetailViewModel(),
                    ViewType.UncompletedSalesView => _createUncompletedSalesViewModel(),
                    ViewType.RevaluationView => _createRevaluationViewModel(),
                    ViewType.ControllerListView => _createControllerListViewModel(),
                    ViewType.UnregisteredSalePanelView => _createUnregisteredSalePanelViewModel(),
                    ViewType.SaleManagerView => _createSaleManagerViewModel(),
                    ViewType.CompletedSalesView => _createCompletedSalesViewModel(),
                    ViewType.CustomReceiptView => _createCustomReceiptViewModel(),
                    ViewType.GlobalReportView => _createGlobalReportViewModel(),
                    ViewType.UserView => _createUserViewModel(),
                    ViewType.DiscountManagement => _createDiscountManagementViewModel(),
                    ViewType.DiscountView => _createDiscountViewModel(),
                    ViewType.FuelDispenser => _createFuelDispenserViewModel(),
                    ViewType.WorkPlaceView => _createWorkplaceSettingsViewModel(),
                    ViewType.NozzleCounterPanelView => _createNozzleCounterPanelViewModel(),
                    _ => throw new ArgumentException("The ViewType does not have a ViewModel.", nameof(viewType)),
                };
            });
        }

        public BaseViewModel CreateViewModel(ViewType viewType)
        {
            return viewType switch
            {
                ViewType.Main => _createMainViewModel(),
                ViewType.Login => _createLoginViewModel(),
                ViewType.Home => _createMainViewModel(),
                ViewType.MainWindow => _createMainWindowViewModel(),
                ViewType.CashView => _createCashViewModel(),
                ViewType.ConfigurationManagementView => _createConfigurationManagementViewModel(),
                ViewType.EventPanelView => _createEventPanelViewModel(),
                ViewType.NozzleCounterPanelView => _createNozzleCounterPanelViewModel(),
                ViewType.FuelSaleView => _createFuelSaleViewModel(),
                ViewType.TanksPanelView => _createTanksPanelViewModel(),
                ViewType.TanksView => _createTanksViewModel(),
                ViewType.FuelIntakeDetailView => _createFuelIntakeDetailViewModel(),
                ViewType.UncompletedSalesView => _createUncompletedSalesViewModel(),
                ViewType.RevaluationView => _createRevaluationViewModel(),
                ViewType.ControllerListView => _createControllerListViewModel(),
                ViewType.UnregisteredSalePanelView => _createUnregisteredSalePanelViewModel(),
                ViewType.SaleManagerView => _createSaleManagerViewModel(),
                ViewType.CompletedSalesView => _createCompletedSalesViewModel(),
                ViewType.CustomReceiptView => _createCustomReceiptViewModel(),
                ViewType.GlobalReportView => _createGlobalReportViewModel(),
                ViewType.UserView => _createUserViewModel(),
                ViewType.DiscountManagement => _createDiscountManagementViewModel(),
                ViewType.DiscountView => _createDiscountViewModel(),
                ViewType.FuelDispenser => _createFuelDispenserViewModel(),
                ViewType.WorkPlaceView => _createWorkplaceSettingsViewModel(),
                _ => throw new ArgumentException("The ViewType does not have a ViewModel.", nameof(viewType)),
            };
        }

        #endregion
    }
}

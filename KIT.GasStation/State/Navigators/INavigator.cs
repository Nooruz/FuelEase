using KIT.GasStation.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KIT.GasStation.State.Navigators
{
    public enum ViewType
    {
        MainWindow,
        Main,
        Login,
        Home,
        CashView,
        ConfigurationManagementView,
        EventPanelView,
        FuelSaleView,
        TanksPanelView,
        UnregisteredSalePanelView,
        NozzleCounterPanelView,
        TanksView,
        FuelIntakeDetailView,
        UncompletedSalesView,
        CompletedSalesView,
        RevaluationView,
        EKassaView,
        ControllerListView,
        SaleManagerView,
        CustomReceiptView,
        GlobalReportView,
        ShiftInfoView,
        UserView,
        DiscountManagement,
        DiscountView,
        FuelDispenser,
        WorkPlaceView,
    }
    public interface INavigator
    {
        BaseViewModel CurrentViewModel { get; set; }
        event Action StateChanged;

        Task PreloadViewModelsAsync(IEnumerable<ViewType> viewTypes);
        Task<BaseViewModel> GetViewModelAsync(ViewType viewType);
        BaseViewModel GetViewModel(ViewType viewType);
        Task Renavigate(ViewType viewType);
    }
}

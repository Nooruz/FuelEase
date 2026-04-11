using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareSettings.CustomControl;
using KIT.GasStation.HardwareSettings.Presenters;

namespace KIT.GasStation.HardwareSettings.Views
{
    public interface IMainView
    {
        #region Events

        event EventHandler AddFuelDispenserClicked;
        event EventHandler AddCashRegisterClicked;
        event EventHandler<PageType> NavigateRequested;

        #endregion

        TreeView TreeViewControl { get; }
        TreeNode ControllersNode { get; }
        TreeNode CashRegistersNode { get; }

        void ShowContent(Control content);
        void AttachPresenter(MainPresenter presenter);
    }
}

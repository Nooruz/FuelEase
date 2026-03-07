using KIT.GasStation.HardwareSettings.CustomControl;
using KIT.GasStation.HardwareSettings.Presenters;

namespace KIT.GasStation.HardwareSettings.Views
{
    public interface IMainView
    {
        #region Events

        event EventHandler AddFuelDispenserClicked;
        event EventHandler<PageType> NavigateRequested;

        #endregion

        void ShowContent(Control content);
        void AttachPresenter(MainPresenter presenter);
    }
}

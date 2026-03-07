using KIT.App.Infrastructure.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.HardwareSettings.CustomControl;
using KIT.GasStation.HardwareSettings.CustomControl.Factories;
using KIT.GasStation.HardwareSettings.Forms;
using KIT.GasStation.HardwareSettings.Models;
using KIT.GasStation.HardwareSettings.Services;
using KIT.GasStation.HardwareSettings.Views;

namespace KIT.GasStation.HardwareSettings.Presenters
{
    public class MainPresenter
    {
        #region Private Members

        private readonly IMainView _view;
        private readonly IPageFactory _pages;
        private readonly IDialogService _dialogService;
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private IPage? _current;

        #endregion

        #region Constructors

        public MainPresenter(IMainView view, 
            IPageFactory pages,
            IDialogService dialogService,
            IHardwareConfigurationService hardwareConfigurationService)
        {
            _view = view;
            _pages = pages;
            _dialogService = dialogService;
            _hardwareConfigurationService = hardwareConfigurationService;

            _view.AttachPresenter(this);
            _view.NavigateRequested += (_, pageType) => Navigate(pageType);

            _view.AddFuelDispenserClicked += View_AddFuelDispenserClicked;
        }

        #endregion

        #region Private Voids

        private void View_AddFuelDispenserClicked(object? sender, EventArgs e)
        {
            IDeviceService<Controller> controllerService = new ControllerService(_hardwareConfigurationService);
            var model = new HardwareModel<Controller, ControllerType>(controllerService);

            _dialogService.Show<HardwareDialog, HardwareModel<Controller, ControllerType>>(model);
        }

        private void Navigate(PageType type)
        {
            _current?.Dispose();
            _current = _pages.Create(type);

            _current.OnShow();
            _view.ShowContent(_current.View);
        }

        #endregion
    }
}

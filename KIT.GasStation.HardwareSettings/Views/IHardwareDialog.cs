using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareSettings.Models;

namespace KIT.GasStation.HardwareSettings.Views
{
    public interface IHardwareDialog
    {
        event EventHandler CreateDeviceClicked;

        void AttachPresenter(HardwareModel<Controller, ControllerType> model);
        void AttachPresenter(HardwareModel<CashRegister, CashRegisterType> model);
    }
}

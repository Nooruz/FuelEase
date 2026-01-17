using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using System.IO.Ports;
using System.Threading.Tasks;

namespace KIT.GasStation.Hardware.Services
{
    public class ControllerService : IDeviceService<Controller>
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;

        #endregion

        #region Constructors

        public ControllerService(IHardwareConfigurationService hardwareConfigurationService)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
        }

        #endregion

        public async Task SaveDeviceAsync(Controller controller)
        {
            switch (controller.Type)
            {
                case ControllerType.Lanfeng:
                    controller.Settings = new LanfengControllerSettings();
                    break;
                case ControllerType.PKElectronics:
                    controller.Settings = new PKElectronicsControllerSettings();
                    break;
                case ControllerType.Gilbarco:
                    controller.Settings = new GilbarcoControllerSettings() { Parity = Parity.Even };
                    break;
            }
            await _hardwareConfigurationService.SaveControllerAsync(controller);
        }
    }
}

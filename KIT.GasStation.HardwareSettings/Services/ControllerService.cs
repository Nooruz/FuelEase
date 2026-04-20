using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using System.IO.Ports;

namespace KIT.GasStation.HardwareSettings.Services
{
    public class ControllerService : IDeviceService<Controller>
    {
        private readonly IHardwareConfigurationService _hardwareConfigurationService;

        public ControllerService(IHardwareConfigurationService hardwareConfigurationService)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
        }

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
                    controller.Settings = new GilbarcoControllerSettings { Parity = Parity.Even };
                    break;
            }
            await _hardwareConfigurationService.SaveControllerAsync(controller);
        }
    }
}

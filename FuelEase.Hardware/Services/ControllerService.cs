using FuelEase.HardwareConfigurations.Models;
using FuelEase.HardwareConfigurations.Services;
using System.Threading.Tasks;

namespace FuelEase.Hardware.Services
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
            }
            await _hardwareConfigurationService.SaveControllerAsync(controller);
        }
    }
}

using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;

namespace KIT.GasStation.HardwareSettings.Services
{
    public class CashRegisterService : IDeviceService<CashRegister>
    {
        private readonly IHardwareConfigurationService _hardwareConfigurationService;

        public CashRegisterService(IHardwareConfigurationService hardwareConfigurationService)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
        }

        public async Task SaveDeviceAsync(CashRegister cashRegister)
        {
            switch (cashRegister.Type)
            {
                case CashRegisterType.EKassa:
                    cashRegister.Settings = new EKassaCashRegisterSettings();
                    break;
                case CashRegisterType.NewCas:
                    cashRegister.Settings = new NewCasCashRegisterSettings();
                    break;
            }
            await _hardwareConfigurationService.SaveCashRegisterAsync(cashRegister);
        }
    }
}

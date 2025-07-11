using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using System.Threading.Tasks;

namespace KIT.GasStation.Hardware.Services
{
    public class CashRegisterService : IDeviceService<CashRegister>
    {
        #region Private Members

        private readonly IHardwareConfigurationService _hardwareConfigurationService;

        #endregion

        #region Constructors

        public CashRegisterService(IHardwareConfigurationService hardwareConfigurationService)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
        }

        #endregion

        public async Task SaveDeviceAsync(CashRegister cashRegister)
        {
            switch (cashRegister.Type)
            {
                case CashRegisterType.None:
                    break;
                case CashRegisterType.EKassa:
                    cashRegister.Settings = new EKassaCashRegisterSettings();
                    break;
                case CashRegisterType.MF:
                    break;
                case CashRegisterType.NewCas:
                    break;
                default:
                    break;
            }
            await _hardwareConfigurationService.SaveCashRegisterAsync(cashRegister);
        }
    }
}

using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.EKassa;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.NewCas;

namespace KIT.App.Infrastructure.Factories
{
    public class CashRegisterFactory : ICashRegisterFactory
    {
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly CreateCashRegister<EKassaCashRegister> _createEKassaCashRegister;
        private readonly CreateCashRegister<NewCasCashRegister> _createNewCasCashRegister;

        public CashRegisterFactory(IHardwareConfigurationService hardwareConfigurationService,
            CreateCashRegister<EKassaCashRegister> createEKassaCashRegister,
            CreateCashRegister<NewCasCashRegister> createNewCasCashRegister)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            _createEKassaCashRegister = createEKassaCashRegister;
            _createNewCasCashRegister = createNewCasCashRegister;
        }

        public ICashRegisterService Create(CashRegisterType cashRegisterType)
        {
            return cashRegisterType switch
            {
                CashRegisterType.EKassa => _createEKassaCashRegister(),
                CashRegisterType.NewCas => _createNewCasCashRegister(),
                _ => throw new NotSupportedException($"Тип кассы {cashRegisterType} не поддерживается."),
            };
        }

        public async Task<ICashRegisterService> CreateAsync(Guid columnId)
        {
            CashRegister? cashRegister = await _hardwareConfigurationService.GetCashRegisterAsync(columnId) ?? throw new ArgumentException($"Касса для колонки с идентификатором {columnId} не найдена.");

            return Create(cashRegister.Type);
        }
    }
}

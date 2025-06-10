using FuelEase.CashRegisters.Services;
using FuelEase.EKassa;
using FuelEase.HardwareConfigurations.Models;
using FuelEase.HardwareConfigurations.Services;

namespace FuelEase.Common.Factories
{
    public class CashRegisterFactory : ICashRegisterFactory
    {
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly CreateCashRegister<EKassaCashRegister> _createEKassaCashRegister;

        public CashRegisterFactory(IHardwareConfigurationService hardwareConfigurationService,
            CreateCashRegister<EKassaCashRegister> createEKassaCashRegister)
        {
            _hardwareConfigurationService = hardwareConfigurationService;
            _createEKassaCashRegister = createEKassaCashRegister;
        }

        public ICashRegisterService Create(CashRegisterType cashRegisterType)
        {
            return cashRegisterType switch
            {
                CashRegisterType.EKassa => _createEKassaCashRegister(),
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

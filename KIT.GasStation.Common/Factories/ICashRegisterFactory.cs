using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.Common.Factories
{
    public interface ICashRegisterFactory
    {
        public ICashRegisterService Create(CashRegisterType cashRegisterType);
        public Task<ICashRegisterService> CreateAsync(Guid columnId);
    }
}

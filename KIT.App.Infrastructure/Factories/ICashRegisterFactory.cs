using KIT.GasStation.CashRegisters.Services;
using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.App.Infrastructure.Factories
{
    public interface ICashRegisterFactory
    {
        public ICashRegisterService Create(CashRegisterType cashRegisterType);
        public Task<ICashRegisterService> CreateAsync(Guid columnId);
    }
}

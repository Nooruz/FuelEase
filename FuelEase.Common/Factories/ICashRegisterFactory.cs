using FuelEase.CashRegisters.Services;
using FuelEase.HardwareConfigurations.Models;

namespace FuelEase.Common.Factories
{
    public interface ICashRegisterFactory
    {
        public ICashRegisterService Create(CashRegisterType cashRegisterType);
        public Task<ICashRegisterService> CreateAsync(Guid columnId);
    }
}

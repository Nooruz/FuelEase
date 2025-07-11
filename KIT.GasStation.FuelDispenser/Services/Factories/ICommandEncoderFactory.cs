using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.FuelDispenser.Services.Factories
{
    public interface ICommandEncoderFactory
    {
        ICommandEncoder Create(ControllerType controllerType);
    }
}

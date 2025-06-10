using FuelEase.HardwareConfigurations.Models;

namespace FuelEase.FuelDispenser.Services.Factories
{
    public interface ICommandEncoderFactory
    {
        ICommandEncoder Create(ControllerType controllerType);
    }
}

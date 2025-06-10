using FuelEase.HardwareConfigurations.Models;

namespace FuelEase.FuelDispenser.Services.Factories
{
    public class CommandEncoderFactory : ICommandEncoderFactory
    {
        public ICommandEncoder Create(ControllerType controllerType)
        {
            return controllerType switch
            {
                ControllerType.Lanfeng => new LanfengCommandEncoder(),
                ControllerType.PKElectronics => new PkElectronicsCommandEncoder(),
                _ => throw new ArgumentException("Unknown controller type")
            };
        }
    }
}

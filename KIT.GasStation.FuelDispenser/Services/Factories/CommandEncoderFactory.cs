using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.FuelDispenser.Services.Factories
{
    public class CommandEncoderFactory : ICommandEncoderFactory
    {
        public ICommandEncoder Create(ControllerType controllerType)
        {
            return controllerType switch
            {
                ControllerType.Lanfeng => new LanfengCommandEncoder(),
                ControllerType.PKElectronics => new PkElectronicsCommandEncoder(),
                ControllerType.TechnoProjekt => new TechnoprojectCommandEncoder(),
                ControllerType.Gilbarco => new GilbarcoCommandEncoder(),
                _ => throw new ArgumentException("Unknown controller type")
            };
        }
    }
}

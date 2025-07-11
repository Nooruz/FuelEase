using KIT.GasStation.Domain.Models;
using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.FuelDispenser.Services.Factories
{
    public interface IProtocolParserFactory
    {
        IProtocolParser CreateIProtocolParser(ControllerType controllerType, List<Nozzle> nozzles);
    }
}

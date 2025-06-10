using FuelEase.Domain.Models;
using FuelEase.HardwareConfigurations.Models;

namespace FuelEase.FuelDispenser.Services.Factories
{
    public interface IProtocolParserFactory
    {
        IProtocolParser CreateIProtocolParser(ControllerType controllerType, List<Nozzle> nozzles);
    }
}

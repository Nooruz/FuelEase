using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.FuelDispenser.Services
{
    public interface IProtocolParser
    {
        byte[] BuildRequest(Command cmd, int controllerAddress, 
            int columnAddress, decimal? value = null, bool bySum = true, LanfengControllerType controllerType = LanfengControllerType.None);
        ControllerResponse ParseResponse(byte[] rawResponse);
    }
}

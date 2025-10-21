using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.FuelDispenser.Services
{
    public interface IProtocolParser
    {
        byte[] BuildRequest(Command cmd, int controllerAddress, int columnAddress, decimal? value = null, decimal? quantity = null);
        ControllerResponse ParseResponse(byte[] rawResponse);
    }
}

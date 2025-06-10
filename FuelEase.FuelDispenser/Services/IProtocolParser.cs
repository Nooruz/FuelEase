using FuelEase.FuelDispenser.Commands;
using FuelEase.FuelDispenser.Models;

namespace FuelEase.FuelDispenser.Services
{
    public interface IProtocolParser
    {
        byte[] BuildRequest(Command cmd, int controllerAddress, int columnAddress, decimal? value = null, decimal? quantity = null);
        DeviceResponse ParseResponse(byte[] rawResponse, Command cmd);
    }
}

using KIT.GasStation.FuelDispenser.Commands;

namespace KIT.GasStation.FuelDispenser.Services
{
    public interface ICommandEncoder
    {
        byte Encode(Command command);
        Command Decode(byte commandByte);
    }
}

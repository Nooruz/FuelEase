using FuelEase.FuelDispenser.Commands;

namespace FuelEase.FuelDispenser.Services
{
    public interface ICommandEncoder
    {
        byte Encode(Command command);
        Command Decode(byte commandByte);
    }
}

using KIT.GasStation.FuelDispenser.Commands;

namespace KIT.GasStation.FuelDispenser.Services
{
    public class GilbarcoCommandEncoder : ICommandEncoder
    {
        // Согласно таблице 2: Command Code + Pump ID (4 бита команды + 4 бита адреса)
        // Здесь энкодер возвращает только код команды (старшие 4 бита)

        private static readonly Dictionary<Command, byte> _commandMap = new()
    {
        { Command.Status,          0x0 }, // Command '0'
        { Command.StartFuelingSum, 0x1 }, // Authorization / Re-Authorize
        { Command.StopFueling,     0x3 }, // Pump Stop
        { Command.CounterSum,      0x4 }, // Request for Transaction Data
        { Command.CounterLiter,    0x5 }, // Request for Pump Totals
        { Command.RealTimeMoney,   0x6 }, // Request for Real-Time Money
        { Command.ChangePrice,     0x2 }, // Data Next (для пресетов, цен и т.д.)
        { Command.StopFueling,     0xF }  // Broadcast All Stop (команда 0xFC)
    };

        private static readonly Dictionary<byte, Command> _reverseMap =
            _commandMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        public byte Encode(Command command)
        {
            if (_commandMap.TryGetValue(command, out byte value))
                return value;
            throw new ArgumentException($"Command '{command}' is not supported by Gilbarco.");
        }

        public Command Decode(byte commandByte)
        {
            // Только для не-специальных команд
            if (_reverseMap.TryGetValue(commandByte, out Command command))
                return command;
            throw new ArgumentException($"Byte '0x{commandByte:X}' is not a standard Gilbarco command.");
        }
    }
}

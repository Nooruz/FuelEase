using KIT.GasStation.FuelDispenser.Commands;

namespace KIT.GasStation.FuelDispenser.Services
{
    public class LanfengCommandEncoder : ICommandEncoder
    {
        #region Private Members

        private static readonly Dictionary<Command, byte> _commandMap = new()
        {
            { Command.Status, 0xB8 },
            { Command.StartFillingSum, 0xA2 },
            { Command.StartFillingQuantity, 0xA3 },
            { Command.StopFilling, 0xB1 },
            { Command.CompleteFilling, 0x8B },
            { Command.ContinueFilling, 0xA1 },
            { Command.ChangePrice, 0xA4 },
            { Command.CounterLiter, 0xAD },
            { Command.CounterSum, 0xAC },
            { Command.FirmwareVersion, 0x1A },
            { Command.ProgramControlMode, 0xA0 },
            { Command.KeyboardControlMode, 0xB0 }
        };

        private static readonly Dictionary<byte, Command> _reverseMap =
            _commandMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        #endregion

        #region Public Voids

        public byte Encode(Command command)
        {
            if (_commandMap.TryGetValue(command, out byte value))
                return value;

            throw new ArgumentException($"Command '{command}' is not supported by Lanfeng.");
        }

        public Command Decode(byte commandByte)
        {
            if (_reverseMap.TryGetValue(commandByte, out Command command))
                return command;

            throw new ArgumentException($"Byte '{commandByte:X2}' is not recognized by Lanfeng.");
        }

        #endregion
    }
}

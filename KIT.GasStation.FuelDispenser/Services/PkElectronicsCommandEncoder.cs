using KIT.GasStation.FuelDispenser.Commands;

namespace KIT.GasStation.FuelDispenser.Services
{
    public class PkElectronicsCommandEncoder : ICommandEncoder
    {
        #region Private Members

        private static readonly Dictionary<Command, byte> _commandMap = new()
        {
            { Command.Status, 0x91 },
            { Command.StartFillingSum, 0x80 },
            { Command.StopFilling, 0x81 },
            { Command.CompleteFilling, 0x8B },
            { Command.ChangePrice, 0x83 },
            { Command.CounterLiter, 0x8D },
            { Command.Sensor, 0xA7 },
            { Command.ReduceCosts, 0x9E },
            { Command.PumpAccelerationTime, 0x9F },
            { Command.Screen, 0x8D },
        };

        private static readonly Dictionary<byte, Command> _reverseMap =
            _commandMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        #endregion

        #region Public Voids

        public byte Encode(Command command)
        {
            if (_commandMap.TryGetValue(command, out byte value))
                return value;

            throw new ArgumentException($"Command '{command}' is not supported by PK Electronics.");
        }

        public Command Decode(byte commandByte)
        {
            if (_reverseMap.TryGetValue(commandByte, out Command command))
                return command;

            throw new ArgumentException($"Byte '{commandByte:X2}' is not recognized by PK Electronics.");
        }

        #endregion
    }
}

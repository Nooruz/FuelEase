using KIT.GasStation.FuelDispenser.Commands;

namespace KIT.GasStation.FuelDispenser.Services
{
    public class TechnoprojectCommandEncoder : ICommandEncoder
    {
        private static readonly Dictionary<Command, byte> _commandMap = new()
        {
            // Соответствие команд протоколу ТехноПроект (ASCII коды)
            { Command.Setup,                (byte)'3' }, // "Установка"
            { Command.Status,               (byte)'4' }, // "Тест" / опрос
            { Command.Start,                (byte)'5' }, // "Пуск"
            { Command.StopFueling,          (byte)'6' }, // "Останов"
            { Command.Reset,                (byte)'7' }, // "Сброс"
            { Command.ReadParams,           (byte)'8' }, // "Параметры"
            { Command.StartFuelingQuantity, (byte)'9' }, // "Задание дозы"
            { Command.SetCounters,          (byte)'A' }  // "Изм. Счетчиков"
        };

        private static readonly Dictionary<byte, Command> _reverseMap =
            _commandMap.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

        public byte Encode(Command command)
        {
            if (_commandMap.TryGetValue(command, out var b))
                return b;
            throw new ArgumentException($"Command '{command}' is not supported by Technoproject.");
        }

        public Command Decode(byte commandByte)
        {
            if (_reverseMap.TryGetValue(commandByte, out var cmd))
                return cmd;
            throw new ArgumentException($"Byte '{commandByte:X2}' is not recognized by Technoproject.");
        }
    }
}

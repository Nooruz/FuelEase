using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Models;
using System.Buffers;

namespace KIT.GasStation.FuelDispenser.Services
{
    /// <summary>
    /// Реализация парсера протокола Lanfeng.
    /// </summary>
    public class LanfengProtocolParser : IProtocolParser
    {
        #region Private Members

        private const byte StartTx = 0xA5;
        private const byte StartRx = 0x5A;
        private LanfengControllerType _lanfengControllerType;
        private readonly ICommandEncoder _commandEncoder;

        #endregion

        #region Constructors

        public LanfengProtocolParser(ICommandEncoder commandEncoder)
        {
            _commandEncoder = commandEncoder;
        }

        #endregion

        #region Public Voids

        public byte[] BuildRequest(Command cmd, int controllerAddress, int columnAddress, decimal? value = null, decimal? quantity = null)
        {
            // Берём буфер из пула длиной 14
            var buffer = ArrayPool<byte>.Shared.Rent(14);
            try
            {
                buffer[0] = StartTx;
                buffer[1] = (byte)(controllerAddress & 0x0F);
                buffer[2] = _commandEncoder.Encode(cmd);

                // Вставить параметры, в зависимости от команды
                switch (cmd)
                {
                    case Command.ChangePrice:
                        AddPriceBytes(buffer, value);
                        break;
                    case Command.StartFillingSum or Command.StartFillingQuantity:
                        AddSumBytes(buffer, value);
                        break;
                        // Для статуса и других команд никаких дополнительных байт
                }

                // Вычисляем контрольную сумму и вставляем в последний байт
                buffer[13] = CalculateChecksum(buffer, length: 13);

                // Возвращаем ровно 14-байтовый массив копией
                var result = new byte[14];
                Array.Copy(buffer, result, 14);
                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        public DeviceResponse ParseResponse(byte[] rawResponse, Command cmd)
        {
            // Базовая валидация
            if (rawResponse == null || rawResponse.Length < 5)
                return new DeviceResponse { IsValid = false };

            // Проверяем стартовый байт
            if (rawResponse[0] != StartRx)
                return new DeviceResponse { IsValid = false };

            // Проверяем контрольную сумму
            var checksum = CalculateChecksum(rawResponse, rawResponse.Length - 1);
            if (checksum != rawResponse[^1])
                return new DeviceResponse { IsValid = false };

            var receivedcmd = _commandEncoder.Decode(rawResponse[2]);
            var address = rawResponse[1] & 0x0F;

            if (receivedcmd != cmd)
                return new DeviceResponse { IsValid = false };

            // Извлекаем статус колонки из 12-го байта (индекс 11)
            byte statusByte = rawResponse[11];
            var (statusAddress, status) = ParseStatusAndAddress(statusByte);

            decimal sumValue = 0m;
            decimal quantityValue = 0m;
            if (receivedcmd == Command.Status)
            {
                // Сумма хранится в байтах 3–6 (BCD), количество – в байтах 7–10 (BCD)
                sumValue = ParseBcdQuantity(rawResponse, offset: 3);
                quantityValue = ParseBcdQuantity(rawResponse, offset: 7);
            }

            return new DeviceResponse
            {
                Address = address,
                Command = cmd,
                Data = rawResponse,
                IsValid = true,
                Status = status,
                Sum = sumValue,
                Quantity = quantityValue,
                StatusAddress = statusAddress,
            };
        }

        #endregion

        #region Private Voids

        /// <summary>
        /// Считывает 4 BCD-байта (offset … offset+3) из rawResponse и возвращает decimal = (BCD-значение) / 100.
        /// </summary>
        private decimal ParseBcdQuantity(byte[] rawResponse, int offset)
        {
            if (rawResponse == null || rawResponse.Length < offset + 4)
                throw new ArgumentException("Недостаточно байт для BCD-парсинга");

            uint combined = 0;

            // Читаем 4 байта подряд
            for (int i = 0; i < 4; i++)
            {
                byte b = rawResponse[offset + i];
                // старший полубайт = десятки, младший = единицы
                uint tens = (uint)((b >> 4) & 0x0F);
                uint units = (uint)(b & 0x0F);
                if (tens > 9 || units > 9)
                    throw new InvalidOperationException($"Некорректный BCD-байт: 0x{b:X2}");

                uint valueOfByte = tens * 10 + units; // от 0 до 99
                combined = combined * 100 + valueOfByte;
            }

            // Теперь combined = целое число «4055» для примера. Делим на 100 → 40.55
            return combined / 100m;
        }

        private static byte CalculateChecksum(byte[] buffer, int length)
        {
            int sum = 0;

            // С 1 до 12 включительно (т.е. индексы 1 по 12)
            for (int i = 1; i <= 12; i++)
            {
                sum -= buffer[i];
            }

            // Приводим результат к байту
            return (byte)sum;
        }

        private static void AddPriceBytes(byte[] buffer, decimal? price)
        {
            if (price == null) return;
            // Пример: цена в тийинах без копеек, 6 цифровых символов ASCII
            int raw = (int)(price * 100);
            var text = raw.ToString().PadLeft(6, '0');
            for (int i = 0; i < 6; i++)
                buffer[3 + i] = (byte)text[i];
        }

        private static void AddSumBytes(byte[] buffer, decimal? sum)
        {
            if (sum == null) return;
            int raw = (int)(sum * 100);
            var text = raw.ToString().PadLeft(6, '0');
            for (int i = 0; i < 6; i++)
                buffer[3 + i] = (byte)text[i];
        }

        /// <summary>
        /// Из 12-го байта ответа (statusByte) извлекает:
        ///   - старшие 4 бита — адрес пистолета (0x1, 0x2, 0x4, 0x8 и т.д.);
        ///   - младшие 4 бита — код статуса (1 = Ready, 2 = PumpWorking, …).
        /// Возвращает сразу и адрес, и статус.
        /// </summary>
        private static (byte nozzleAddress, NozzleStatus status) ParseStatusAndAddress(byte statusByte)
        {
            // Адрес пистолета — в старших 4 битах
            byte nozzleAddress = (byte)((statusByte >> 4) & 0x0F);

            // Код статуса — в младших 4 битах
            byte statusCode = (byte)(statusByte & 0x0F);

            NozzleStatus status = statusCode switch
            {
                1 => NozzleStatus.Ready,
                2 => NozzleStatus.PumpWorking,
                3 => NozzleStatus.WaitingStop,
                4 => NozzleStatus.PumpStop,
                5 => NozzleStatus.WaitingRemoved,
                _ => NozzleStatus.Unknown
            };

            return (nozzleAddress, status);
        }

        #endregion
    }
}

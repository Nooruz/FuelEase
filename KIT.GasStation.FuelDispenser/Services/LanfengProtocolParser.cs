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
        private const byte FrameLength = 14;
        private readonly ICommandEncoder _commandEncoder;
        private LanfengControllerType _controllerType;

        #endregion

        #region Constructors

        public LanfengProtocolParser(ICommandEncoder commandEncoder)
        {
            _commandEncoder = commandEncoder;
        }

        #endregion

        #region Public Voids

        public byte[] BuildRequest(Command cmd, int controllerAddress, 
            int columnAddress = 0, decimal? value = null, bool bySum = true,
            LanfengControllerType controllerType = LanfengControllerType.Single)
        {
            // Берём буфер из пула длиной 14
            var rented = ArrayPool<byte>.Shared.Rent(FrameLength);
            try
            {
                // работаем строго с окном [0..13]
                var frame = rented.AsSpan(0, FrameLength);

                // ОБЯЗАТЕЛЬНО: очистить, чтобы не тянуть мусор
                frame.Clear();

                frame[0] = StartTx;
                switch (controllerType)
                {
                    case LanfengControllerType.Single:
                        frame[1] = (byte)(0 << 4 | controllerAddress);
                        break;
                    case LanfengControllerType.Multi:
                        frame[1] = (byte)(columnAddress << 4 | controllerAddress);
                        break;
                }
                frame[2] = _commandEncoder.Encode(cmd);

                // Вставить параметры, в зависимости от команды
                switch (cmd)
                {
                    case Command.ChangePrice:
                        AddPriceBytes(rented, value);
                        break;
                    case Command.StartFuelingSum or Command.StartFuelingQuantity:
                        AddSumBytes(rented, value, bySum);
                        break;
                        // Для статуса и других команд никаких дополнительных байт
                }

                // Вычисляем контрольную сумму и вставляем в последний байт
                frame[13] = CalculateChecksum(rented, length: 13);

                // Возвращаем ровно 14-байтовый массив копией
                var result = new byte[14];
                Buffer.BlockCopy(rented, 0, result, 0, FrameLength);
                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            }
        }

        public ControllerResponse ParseResponse(byte[] rawResponse)
        {
            // Базовая валидация
            if (rawResponse == null || rawResponse.Length < FrameLength)
                return new ControllerResponse { IsValid = false };

            // Проверяем стартовый байт
            if (rawResponse[0] != StartRx)
            {
                if (!TryAlignAndExtractFrame(rawResponse, out var aligned))
                    return new ControllerResponse { IsValid = false };

                rawResponse = aligned;
            }

            // Проверяем контрольную сумму
            var checksum = CalculateChecksum(rawResponse, rawResponse.Length - 1);
            if (checksum != rawResponse[^1])
                return new ControllerResponse { IsValid = false };

            var receivedcmd = _commandEncoder.Decode(rawResponse[2]);
            var address = (rawResponse[1] >> 4) & 0x0F;

            // Извлекаем статус колонки из 12-го байта (индекс 11)
            byte statusByte = rawResponse[11];
            var (statusAddress, status) = ParseStatusAndAddress(statusByte);

            decimal sumValue = 0m;
            decimal quantityValue = 0m;

            // Сумма хранится в байтах 3–6 (BCD), количество – в байтах 7–10 (BCD)
            sumValue = ParseBcdQuantity(rawResponse, offset: 3);
            quantityValue = ParseBcdQuantity(rawResponse, offset: 7);

            if (receivedcmd == Command.FirmwareVersion)
            {
                _controllerType = (LanfengControllerType)rawResponse[3];
            }

            if (receivedcmd == Command.CounterLiter)
            {
                quantityValue = ParseBcdQuantity(rawResponse, offset: 6, endIndex: 5);
            }

            return new ControllerResponse
            {
                Address = address,
                Command = receivedcmd,
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
        private decimal ParseBcdQuantity(byte[] rawResponse, int offset, int endIndex = 4)
        {
            if (rawResponse == null || rawResponse.Length < offset + 4)
                throw new ArgumentException("Недостаточно байт для BCD-парсинга");

            uint combined = 0;

            // Читаем 4 байта подряд
            for (int i = 0; i < endIndex; i++)
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
            // 1) Умножаем на 100 — убираем копейки (12.34 -> 1234)
            int intValue = (int)(price * 100);

            // 2) Формируем шестизначную строку (001234)
            string strValue = intValue.ToString("D6");

            // 3) Преобразуем каждые 2 цифры в один HEX-байт
            for (int i = 0; i < 3; i++)
            {
                string pair = strValue.Substring(i * 2, 2); // "00", "12", "34"
                buffer[3 + i] = Convert.ToByte(pair, 16);
            }
        }

        private static void AddSumBytes(byte[] buffer, decimal? sum, bool bySum)
        {
            if (sum == null) return;
            // 1) Умножаем на 100 — убираем копейки (12.34 -> 1234)
            int intValue = (int)(sum * 100);

            // 2) Формируем шестизначную строку (00001234)
            string strValue = intValue.ToString("D8");

            // 3) Выбираем сдвиг в зависимости от режима
            int startIndex = bySum ? 3 : 6;

            // 4) Преобразуем каждые 2 цифры в один HEX-байт
            for (int i = 0; i < 4; i++)
            {
                string pair = strValue.Substring(i * 2, 2); // "00", "00", "12", "34"
                buffer[startIndex + i] = Convert.ToByte(pair, 16);
            }
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

        //Метод для выравнивания и извлечения кадра из необработанного ответа
        private bool TryAlignAndExtractFrame(byte[] rawResponse, out byte[] sorted)
        {
            sorted = Array.Empty<byte>();

            int idx = Array.IndexOf(rawResponse, StartRx);
            if (idx < 0)
                return false; // стартовый байт не найден

            int len = rawResponse.Length;
            sorted = new byte[len];

            // копируем хвост (с найденного индекса до конца)
            Array.Copy(rawResponse, idx, sorted, 0, len - idx);

            // копируем голову (всё, что было до стартового байта)
            Array.Copy(rawResponse, 0, sorted, len - idx, idx);

            return true;
        }

        #endregion
    }
}

using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Models;
using System.Buffers;
using System.Text;

namespace KIT.GasStation.FuelDispenser.Services
{
    /// <summary>
    /// Парсер протокола ТехноПроект (см. Протокол_УЗСГ.doc).
    /// Формат кадров: 23 байта
    /// 0  SOH  (0x01)
    /// 1-2 TRK_No (2 ASCII)
    /// 3   Command (ASCII)
    /// 4   STX (0x02)
    /// 5-10 Price (6 ASCII) - копейки
    /// 11-16 Volume (6 ASCII) - мл
    /// 17  Status (1 byte)
    /// 18  Status2 (1 byte) - дополнительный
    /// 19-20 Sost (2 ASCII)
    /// 21  ETX (0x03)
    /// 22  CRC (XOR от байта 1 до 21)
    /// </summary>
    
    public class TechnoprojectProtocolParser
    {
        private const byte SOH = 0x01;
        private const byte STX = 0x02;
        private const byte ETX = 0x03;
        private const int FrameLength = 23;

        private readonly ICommandEncoder _encoder;

        public TechnoprojectProtocolParser(ICommandEncoder encoder)
        {
            _encoder = encoder;
        }

        public byte[] BuildRequest(Command cmd, int controllerAddress, int columnAddress = 0, decimal? value = null, bool bySum = true)
        {
            var rented = ArrayPool<byte>.Shared.Rent(FrameLength);
            try
            {
                var frame = rented.AsSpan(0, FrameLength);
                frame.Clear();

                frame[0] = SOH;

                // TRK_No — два ASCII-символа (01..127). Формируем как двухсимвольный десятичный номер.
                int addr = Math.Clamp(controllerAddress, 1, 127);
                var trk = addr.ToString("D2");
                frame[1] = (byte)trk[0];
                frame[2] = (byte)trk[1];

                // Команда (ASCII)
                frame[3] = _encoder.Encode(cmd);

                // STX
                frame[4] = STX;

                // Price (5..10) и Volume (11..16) — ASCII '0'..'9' длиной 6
                // В зависимости от команды помещаем значения
                if (cmd == Command.StartFuelingQuantity)
                {
                    // Если bySum == true — кладём сумму в Price (копейки)
                    if (value.HasValue)
                    {
                        if (bySum)
                        {
                            var priceStr = ConvertPriceToSixAscii(value.Value); // копейки
                            WriteAsciiToSpan(frame.Slice(5, 6), priceStr);
                            // Volume = "000000"
                            WriteAsciiToSpan(frame.Slice(11, 6), "000000");
                        }
                        else
                        {
                            // по количеству — клали в Volume (мл)
                            var ml = ConvertQuantityToMlInt(value.Value);
                            WriteAsciiToSpan(frame.Slice(11, 6), ml.ToString("D6"));
                            WriteAsciiToSpan(frame.Slice(5, 6), "000000");
                        }
                    }
                }
                else if (cmd == Command.Setup)
                {
                    // Используем Setup для установки цены (как в документе)
                    if (value.HasValue)
                    {
                        var priceStr = ConvertPriceToSixAscii(value.Value);
                        WriteAsciiToSpan(frame.Slice(5, 6), priceStr);
                    }
                }
                else
                {
                    // Для всех прочих команд оставляем поля нулевыми
                    WriteAsciiToSpan(frame.Slice(5, 6), "000000");
                    WriteAsciiToSpan(frame.Slice(11, 6), "000000");
                }

                // Status / Status2 / Sost — по умолчанию "00", "00", "00"
                frame[17] = (byte)'0';
                frame[18] = (byte)'0';
                frame[19] = (byte)'0';
                frame[20] = (byte)'0';

                // ETX
                frame[21] = ETX;

                // CRC: XOR всех байт с 1 по 21 включительно
                frame[22] = CalculateChecksum(frame.ToArray(), 1, 21);

                // Возвращаем ровно 23-байтовый массив
                var result = new byte[FrameLength];
                Buffer.BlockCopy(rented, 0, result, 0, FrameLength);
                return result;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rented, clearArray: true);
            }
        }

        //public ControllerResponse ParseResponse(byte[] raw)
        //{
        //    if (raw == null || raw.Length < FrameLength)
        //        return new ControllerResponse { IsValid = false };

        //    // Выравниваем — ищем SOH
        //    if (raw[0] != SOH)
        //    {
        //        if (!TryAlign(raw, out var aligned))
        //            return new ControllerResponse { IsValid = false };
        //        raw = aligned;
        //    }

        //    if (raw.Length < FrameLength) return new ControllerResponse { IsValid = false };

        //    // Проверяем ETX
        //    if (raw[21] != ETX) return new ControllerResponse { IsValid = false };

        //    // Проверяем CRC
        //    var crc = CalculateChecksum(raw, 1, 21);
        //    if (raw[22] != crc) return new ControllerResponse { IsValid = false };

        //    // Читаем поля
        //    int address = ParseTrkNo(raw[1], raw[2]);
        //    var command = _encoder.Decode(raw[3]);

        //    var priceAscii = Encoding.ASCII.GetString(raw, 5, 6);
        //    var volumeAscii = Encoding.ASCII.GetString(raw, 11, 6);

        //    decimal price = ParsePriceFromAscii(priceAscii);   // копейки -> рубли
        //    decimal quantity = ParseVolumeFromAscii(volumeAscii); // мл -> литры

        //    // Sost
        //    string sost = Encoding.ASCII.GetString(raw, 19, 2);
        //    NozzleStatus status = MapSostToNozzleStatus(sost);

        //    // Доп. код статуса/адреса можно извлечь из raw[17] / raw[18] по необходимости
        //    byte statusByte = raw[17];
        //    byte additional = raw[18];

        //    return new ControllerResponse
        //    {
        //        Address = address,
        //        Command = command,
        //        Data = raw,
        //        IsValid = true,
        //        Status = status,
        //        Sum = price,
        //        Quantity = quantity,
        //        StatusAddress = (byte)(statusByte & 0x0F) // приблизительное значение (низкий ниббл)
        //    };
        //}

        #region Helpers

        private static void WriteAsciiToSpan(Span<byte> span, string s)
        {
            for (int i = 0; i < span.Length && i < s.Length; i++)
                span[i] = (byte)s[i];
        }

        private static string ConvertPriceToSixAscii(decimal value)
        {
            // ожидаем value в рублях; переводим в копейки (целое)
            int kop = (int)Math.Round(value * 100m);
            return kop.ToString("D6");
        }

        private static int ConvertQuantityToMlInt(decimal value)
        {
            // если value — литры, переводим в мл
            return (int)Math.Round(value * 1000m);
        }

        private static int ParseTrkNo(byte b1, byte b2)
        {
            var s = $"{(char)b1}{(char)b2}";
            if (int.TryParse(s, out int v)) return v;
            return 0;
        }

        private static decimal ParsePriceFromAscii(string ascii6)
        {
            if (!int.TryParse(ascii6, out int kop)) return 0m;
            return kop / 100m; // копейки -> рубли
        }

        private static decimal ParseVolumeFromAscii(string ascii6)
        {
            if (!int.TryParse(ascii6, out int ml)) return 0m;
            return ml / 1000m; // мл -> литры
        }

        private static NozzleStatus MapSostToNozzleStatus(string sost)
        {
            // согласно протоколу: ASCII '1' - задание дозы, '3' - пуск, '4'/'7' - останов, '5' - сброс
            if (string.IsNullOrEmpty(sost)) return NozzleStatus.Unknown;
            char c = sost[0];
            return c switch
            {
                '1' => NozzleStatus.Ready,
                '3' => NozzleStatus.PumpWorking,
                '4' or '7' => NozzleStatus.PumpStop,
                '5' => NozzleStatus.WaitingRemoved,
                _ => NozzleStatus.Unknown
            };
        }

        private static byte CalculateChecksum(byte[] buffer, int startInclusive, int endInclusive)
        {
            byte result = 0x00;
            for (int i = startInclusive; i <= endInclusive && i < buffer.Length; i++)
            {
                result ^= buffer[i];
            }
            return result;
        }

        private static bool TryAlign(byte[] raw, out byte[] aligned)
        {
            aligned = Array.Empty<byte>();
            int idx = Array.IndexOf(raw, SOH);
            if (idx < 0) return false;
            if (raw.Length - idx < FrameLength) return false;

            aligned = new byte[FrameLength];
            Array.Copy(raw, idx, aligned, 0, FrameLength);
            return true;
        }

        public byte[] BuildRequest(Command cmd, int controllerAddress, int columnAddress, decimal? value = null, bool bySum = true, LanfengControllerType controllerType = LanfengControllerType.None)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}

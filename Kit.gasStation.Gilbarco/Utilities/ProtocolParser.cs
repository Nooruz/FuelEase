using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.HardwareConfigurations.Models;
using System.Globalization;

namespace KIT.GasStation.Gilbarco.Utilities
{
    /// <summary>
    /// Инструментарий для сборки и парсинга низкоуровневых команд протокола Gilbarco TWOTP.
    /// </summary>
    public static class ProtocolParser
    {
        #region Константы протокола

        private const byte CommandMask = 0xF0;
        private const byte AddressMask = 0x0F;

        private const byte DataWordPrefix = 0xE0;
        private const byte DataControlPrefix = 0xF0;

        private const byte StartOfText = 0xFF; // F F = STX
        private const byte EndOfText = 0xF0; // F 0 = ETX

        private const byte DataControl_VolumePreset = 0xF1;
        private const byte DataControl_MoneyPreset = 0xF2;
        private const byte DataControl_Level1 = 0xF4;
        private const byte DataControl_Level2 = 0xF5;
        private const byte DataControl_GradeNext = 0xF6;
        private const byte DataControl_PpuNext = 0xF7;
        private const byte DataControl_PresetAmountNext = 0xF8;
        private const byte DataControl_LrcNext = 0xFB;
        private const byte SpecialFunctionModeNext = 0xFE;

        #endregion

        #region Публичный API



        #endregion

        /// <summary>
        /// Извлекает код статуса из байта ответа (старшие 4 бита) [3].
        /// </summary>
        /// <param name="response">Байт, полученный от ТРК.</param>
        public static GilbarcoStatus GetStatus(byte[] response)
        {
            if (response == null || response.Length == 0)
                return GilbarcoStatus.DataError;

            // Если пришло 2 байта (эхо + статус), берем второй [1]. 
            // Если 1 байт — берем его.
            byte statusByte = response.Length >= 2 ? response[1] : response[0];

            // Извлекаем старший ниббл (сдвиг на 4 бита вправо) [2]
            return (GilbarcoStatus)((statusByte >> 4) & 0x0F);
        }

        /// <summary>
        /// Извлекает адрес ТРК из байта ответа (младшие 4 бита) [3].
        /// </summary>
        /// <remarks>Согласно протоколу, значение 0 соответствует ТРК №16 [4].</remarks>
        public static int GetPumpId(byte responseByte)
        {
            int id = responseByte & 0x0F;
            return id == 0 ? 16 : id;
        }

        /// <summary>
        /// Формирует однобайтовую команду для ТРК.
        /// </summary>
        /// <param name="cmd">Тип команды.</param>
        /// <param name="pumpId">Номер ТРК (1-16).</param>
        public static byte[] BuildPackCommand(Command cmd, int pumpId)
        {
            byte addr = (byte)(pumpId == 16 ? 0 : pumpId & 0x0F); // 16 кодируется как 0 [1, 4]
            byte value = (byte)(((byte)cmd << 4) | addr);

            return new[] { value };
        }

        public static NozzleStatus GilbarcoStatusToNozzleStatusConverter(GilbarcoStatus status)
        {
            switch (status)
            {
                case GilbarcoStatus.DataError:
                    break;
                case GilbarcoStatus.Off:
                    return NozzleStatus.Ready;
                case GilbarcoStatus.Call:
                    return NozzleStatus.Ready;
                case GilbarcoStatus.AuthorizedNotDelivering:
                    return NozzleStatus.WaitingRemoved;
                case GilbarcoStatus.Busy:
                    return NozzleStatus.PumpWorking;
                case GilbarcoStatus.TransactionCompletePeot:
                    return NozzleStatus.PumpStop;
                case GilbarcoStatus.TransactionCompleteFeot:
                    return NozzleStatus.PumpStop;
                case GilbarcoStatus.PumpStop:
                    return NozzleStatus.PumpStop;
                case GilbarcoStatus.SendData:
                    break;
                default:
                    break;
            }

            return NozzleStatus.Unknown;
        }

        /// <summary>
        /// Формирует блок данных для запроса расширенного статуса (Special Function 010).
        /// </summary>
        /// <returns>Массив байтов: FF-E9-FE-E0-E1-E0-FB-EE-F0</returns>
        public static byte[] BuildExtendedStatusBlock()
        {
            // 0xFE - префикс специальной функции [3, 4]
            // 0xE0, 0xE1, 0xE0 - код функции 010 в формате Gilbarco [5, 6]
            return BuildDataBlock(new byte[] { SpecialFunctionModeNext, 0xE0, 0xE1, 0xE0 });
        }

        /// <summary>
        /// Удаляет эхо-команду из начала ответа устройства.
        /// Многие ТРК сначала возвращают отправленную команду (эхо),
        /// а затем полезные данные ответа. Метод проверяет, что ответ
        /// начинается с байтов команды, и если да — отрезает их.
        /// </summary>
        /// <param name="response">Полный ответ от устройства (с эхом).</param>
        /// <param name="command">Команда, которая была отправлена устройству.</param>
        /// <returns>
        /// Массив байтов без эхо-команды.  
        /// Если эхо не найдено в начале ответа, возвращается исходный массив.
        /// </returns>
        public static byte[] RemoveEcho(byte[] response, byte[] command)
        {
            if (response == null)
                throw new ArgumentNullException(nameof(response));

            if (command == null)
                throw new ArgumentNullException(nameof(command));

            // Если ответ короче команды — эха быть не может
            if (response.Length < command.Length)
                return response;

            // Проверяем, что начало ответа полностью совпадает с командой
            if (response.AsSpan(0, command.Length).SequenceEqual(command))
            {
                // Отрезаем команду и возвращаем только полезную часть
                var result = new byte[response.Length - command.Length];
                Buffer.BlockCopy(response, command.Length, result, 0, result.Length);
                return result;
            }

            // Эхо не найдено — возвращаем ответ как есть
            return response;
        }

        /// <summary>
        /// Разбирает расширенный статус ТРК Gilbarco из сырого массива байтов,
        /// полученного в ответ на команду Extended Status.
        /// </summary>
        /// <param name="raw">
        /// Сырой массив байтов ответа без эхо-команды.
        /// Пример: BA-B0-B3-B1-B0-B1-B0-B0-B0-B0-B1-B1-B1-B1-B3-C2-B8-8D-8A
        /// </param>
        /// <returns>
        /// Заполненный объект <see cref="GilbarcoExtendedStatus"/>.
        /// </returns>
        /// <remarks>
        /// Разбор полей согласно разделу 4.9.4.4 спецификации Gilbarco.
        /// Индексация:
        /// raw[6]  = идентификатор колонки (Pump ID)
        /// raw[7-9]= код функции
        /// raw[10-15] = данные статуса
        /// </remarks>
        public static GilbarcoExtendedStatus ParseExtendedStatus(byte[] raw)
        {
            if (raw == null) throw new ArgumentNullException(nameof(raw));
            if (raw.Length < 19)
                throw new ArgumentException("Extended Status frame is too short.", nameof(raw));

            // Индексы по разбору:
            // 0   BA  : prefix
            // 1-2     : block length
            // 3       : pump number
            // 4-6     : function echo (010)
            // 7-8     : remaining blocks
            // 9       : MSD
            // 10      : Price Level Selection Needed
            // 11      : Grade Selection Needed
            // 12      : Nozzle (handle)
            // 13      : Push-To-Start Needed
            // 14      : Selected Grade
            // 15-16   : LRC
            // 17      : CR
            // 18      : LF

            int pumpId = raw[3] - 0xB0;

            // По примеру:
            // B1 = 1 (Not Needed) для PriceLevel/Grade/PushToStart
            // Для "Needed" удобно хранить как bool "Needed": (value == 0)
            int priceLevelVal = raw[10] - 0xB0;
            int gradeSelVal = raw[11] - 0xB0;
            int nozzleVal = raw[12] - 0xB0;
            int pushStartVal = raw[13] - 0xB0;
            int selectedGrade = raw[14] - 0xB0;

            return new GilbarcoExtendedStatus
            {
                PumpId = pumpId,

                // true = требуется, false = не требуется
                PriceLevelNeeded = priceLevelVal == 0,
                GradeSelectionNeeded = gradeSelVal == 0,

                // В твоём описании: 1 = снят (On/Out)
                IsNozzleLifted = nozzleVal == 1,

                // true = требуется нажать старт
                PushToStartNeeded = pushStartVal == 0,

                SelectedGrade = selectedGrade
            };
        }

        /// <summary>
        /// Разбирает ответ SF 00E и возвращает конфигурацию колонки.
        /// </summary>
        /// <param name="response">
        /// Сырой ответ от колонки Gilbarco (BX/CX-кодировка).
        /// </param>
        /// <returns>Распарсенная конфигурация колонки.</returns>
        public static MiscPumpConfig ParseConfig(byte[] response)
        {
            // BX -> цифра 0..9
            static int Digit(byte b) => b - 0xB0;

            // Две BX-цифры -> число 00..99
            int ToInt(int index) => Digit(response[index]) * 10 + Digit(response[index + 1]);

            // В твоих ответах SF полезная нагрузка 00E начинается с 9-го байта
            const int dataStart = 9;

            return new MiscPumpConfig
            {
                UnitType = (GilbarcoUnitType)ToInt(dataStart + 0),   // 9..10
                VolumeUnit = (GilbarcoVolumeUnit)ToInt(dataStart + 2), // 11..12

                // Reserved x6 = 6 байт = 3 пары (13..18) :contentReference[oaicite:5]{index=5}

                MoneyMode = (GilbarcoMoneyMode)ToInt(dataStart + 10), // 19..20
                AutoOnMode = (GilbarcoAutoOnMode)ToInt(dataStart + 12) // 21..22
            };
        }

        public static RequestTransactionData ParseRequestTransactionData(byte[] response)
        {
            if (response == null || response.Length == 0)
                throw new ArgumentException("Пустой ответ.");

            // 1) Найти рамку FF ... F0
            int start = Array.IndexOf(response, (byte)0xFF);
            int end = Array.LastIndexOf(response, (byte)0xF0);

            byte[] frame = response;

            if (start >= 0 && end > start)
            {
                int len = end - start + 1;
                frame = new byte[len];
                Buffer.BlockCopy(response, start, frame, 0, len);
            }

            int i = 0;

            // пропускаем 21 / FF / F1 если есть
            while (i < frame.Length &&
                   (frame[i] == 0x21 || frame[i] == 0xFF || frame[i] == 0xF1))
                i++;

            byte? idType = null;
            int? position = null;
            int? error = null;
            int? columnType = null;
            int? grade = null;
            decimal? price = null;
            decimal? volume = null;
            decimal? amount = null;

            while (i < frame.Length)
            {
                byte tag = frame[i++];

                if (tag == 0xF0 || tag == 0xFB)
                    break;

                // ---------- F8 блок ----------
                if (tag == 0xF8)
                {
                    var data = ReadUntilNextTag(frame, ref i);

                    if (data.Length > 0)
                        idType = data[0];

                    if (data.Length >= 2 && IsExDigit(data[1]))
                        position = ExDigit(data[1]) + 1;

                    if (data.Length >= 3 && IsExDigit(data[2]))
                        error = ExDigit(data[2]);

                    if (data.Length >= 5 && IsExDigit(data[4]))
                        columnType = ExDigit(data[4]);
                }

                // ---------- F6 Grade ----------
                else if (tag == 0xF6)
                {
                    var data = ReadUntilNextTag(frame, ref i);

                    if (data.Length >= 1 && IsExDigit(data[0]))
                        grade = ExDigit(data[0]) + 1;
                }

                // ---------- F7 Price (4 цифры) ----------
                else if (tag == 0xF7)
                {
                    var digits = ReadUntilNextTag(frame, ref i);
                    price = ParseExLsdDecimal(digits, 4, 2);
                }

                // ---------- F9 Volume (6 цифр) ----------
                else if (tag == 0xF9)
                {
                    var digits = ReadUntilNextTag(frame, ref i);
                    volume = ParseExLsdDecimal(digits, 6, 3);
                }

                // ---------- FA Money (6 цифр) ----------
                else if (tag == 0xFA)
                {
                    var digits = ReadUntilNextTag(frame, ref i);
                    amount = ParseExLsdDecimal(digits, 6, 2);
                }
            }

            return new RequestTransactionData
            {
                IdBlockTypeRaw = idType,
                PositionNumber = position,
                ErrorCode = error,
                ColumnType = columnType,
                Grade = grade,
                Price = price,
                Volume = volume,
                Amount = amount
            };
        }

        /// <summary>
        /// Формирует блок данных для запроса конфигурации ТРК (Special Function 00E).
        /// </summary>
        /// <returns>Массив байтов для отправки после получения статуса SendData (D).</returns>
        public static byte[] BuildMiscPumpDataBlock()
        {
            // Передаем только полезную нагрузку: 
            // FE (Special Function Next) + код 00E (E0, E0, EE)
            return BuildDataBlock(new byte[] { SpecialFunctionModeNext, 0xEE, 0xE0, 0xE0 });
        }

        public static decimal ParseRealTimeMoney(byte[] dataWords)
        {
            long raw = 0;
            long factor = 1;

            for (int i = 0; i < dataWords.Length; i++)
            {
                raw += (dataWords[i] & 0x0F) * factor; // E0..E9 -> 0..9
                factor *= 10;
            }

            return raw / 1000m; // XXX.XXX
        }

        /// <summary>
        /// Формирует блок данных для изменения цены (Price Change Data).
        /// </summary>
        /// <param name="column">Выбранный пистолет</param>
        /// <param name="price">Цена (например, 65.50)</param>
        public static byte[] BuildPriceChangeBlock(Column column, decimal price)
        {
            if (column.Settings is GilbarcoColumnSettings settings)
            {
                // 1) Масштабируем цену под десятичную точку ТРК
                int scale = Pow10(settings.PriceDecimalPoint);

                int scaled = (int)Math.Round(price * scale, MidpointRounding.AwayFromZero);

                if (scaled < 0 || scaled > 9999)
                    throw new ArgumentOutOfRangeException(nameof(price),
                        $"Цена {price} с decimals={settings.PriceDecimalPoint} не помещается в 4 цифры PPU (XXXX). Получилось: {scaled}.");

                // 2) Формируем payload
                var payload = new List<byte>
                {
                    // Уровень цены (обычно Level 1)
                    DataControl_Level1,

                    // Выбор сорта
                    0xF6,
                    (byte)(0xE0 | (column.Nozzle - 1)), // 1-16 → 0-F

                    // Маркер PPU
                    0xF7
                };

                // 3) Всегда 4 цифры с ведущими нулями
                string digits = scaled.ToString("D4", CultureInfo.InvariantCulture);
                // пример:
                // 7250 → "7250"
                // 725  → "0725"
                // 65   → "0065"

                // 4) Добавляем в формате LSD first
                payload.Add((byte)(0xE0 | (digits[3] - '0'))); // LSD
                payload.Add((byte)(0xE0 | (digits[2] - '0')));
                payload.Add((byte)(0xE0 | (digits[1] - '0')));
                payload.Add((byte)(0xE0 | (digits[0] - '0'))); // MSD

                // 5) Оборачиваем в стандартный блок
                return BuildDataBlock(payload);
            }
            return Array.Empty<byte>();
        }

        /// <summary>
        /// Расшифровывает стандартный ответ Gilbarco TWOWIRE на команду 0x5 (Pump Totals).
        /// </summary>
        /// <remarks>
        /// Общая структура кадра:
        /// FF | [Блоки сортов по 30 байт] | FB | LRC | F0
        ///
        /// Каждый блок (30 байт):
        /// F6 EX  → номер пистолета
        /// F9     → объём (8 BCD цифр, LSD first)
        /// FA     → деньги (8 BCD цифр, LSD first)
        /// F4     → цена Level 1 (4 BCD цифры)
        /// F5     → цена Level 2 (4 BCD цифры)
        /// </remarks>
        /// <param name="data">Сырой массив байт, полученный от ТРК.</param>
        /// <returns>Список данных по каждому пистолету.</returns>
        public static List<GradeData> ParseCounters(byte[] data)
        {
            var result = new List<GradeData>();

            // Проверка на минимальную длину кадра
            if (data == null || data.Length < 4)
                return result;

            // Проверка стартового байта
            if (data[0] != StartOfText)
                return result;

            // Проверка завершающих байт FB LRC F0
            int etxIndex = data.Length - 3;
            if (data[etxIndex] != DataControl_LrcNext || data[^1] != EndOfText)
                return result;

            byte receivedLrc = data[^2];

            byte calculatedLrc = CalculateLrc(data.Take(etxIndex + 1)); // включаем FF и FB
            if (calculatedLrc != receivedLrc)
                return result;

            // Полезная нагрузка без FF и хвоста FB-LRC-F0
            int payloadLength = data.Length - 4;

            // Каждый сорт занимает ровно 30 байт
            if (payloadLength % 30 != 0)
                return result;

            int gradeCount = payloadLength / 30;

            // Парсим каждый блок сорта
            for (int i = 0; i < gradeCount; i++)
            {
                int offset = 1 + (i * 30);

                // Проверка маркеров протокола
                if (data[offset] != 0xF6) continue;
                if (data[offset + 2] != 0xF9) continue;
                if (data[offset + 11] != 0xFA) continue;
                if (data[offset + 20] != 0xF4) continue;
                if (data[offset + 25] != 0xF5) continue;

                int nozzle = (data[offset + 1] & 0x0F) + 1;

                long counterRaw = DecodeLsdBcd(data, offset + 3, 8);
                long moneyRaw = DecodeLsdBcd(data, offset + 12, 8);
                int priceL1 = (int)DecodeLsdBcd(data, offset + 21, 4);
                int priceL2 = (int)DecodeLsdBcd(data, offset + 26, 4);

                result.Add(new GradeData
                {
                    Nozzle = nozzle,
                    Counter = counterRaw / 100m,
                    Money = moneyRaw,
                    PriceLevel1 = priceL1,
                    PriceLevel2 = priceL2
                });
            }

            return result;
        }

        public static byte[] BuildPresetBlock(Controller controller, Column column, int grade, decimal amount, bool bySum)
        {
            if (controller.Settings is not GilbarcoControllerSettings controllerSettings)
                return Array.Empty<byte>();

            if (column.Settings is not GilbarcoColumnSettings colSettings)
                return Array.Empty<byte>();

            // 1) Определяем длину пресета по деньгам: 5 или 6 цифр
            int digitsCount = controllerSettings.PumpConfig.MoneyMode switch
            {
                GilbarcoMoneyMode.FiveDigits => 5,
                GilbarcoMoneyMode.SixDigits => 6,
                _ => 5
            };

            // 2) Выбираем тип пресета
            // bySum=true  -> Money preset (F2)
            // bySum=false -> Volume preset (F1)
            byte presetControl = bySum ? DataControl_MoneyPreset : DataControl_VolumePreset;

            // 3) Масштабируем amount в целое (LSD-first цифры)
            // ВАЖНО: ведущие нули должны быть СЛЕВА (D5/D6), чтобы разряды не съезжали.
            // Пример по твоему ожиданию:
            // amount=100.00, decimals=2 => scaled=10000 => "10000" => E0 E0 E0 E0 E1
            // amount=100.00, decimals=1 => scaled=1000  => "01000" => E0 E0 E0 E1 E0
            int scale = Pow10(colSettings.PriceDecimalPoint);
            long scaled = (long)Math.Round(amount * scale, MidpointRounding.AwayFromZero);

            long max = digitsCount == 5 ? 99_999 : 999_999;
            if (scaled < 0 || scaled > max)
                throw new ArgumentOutOfRangeException(nameof(amount),
                    $"Preset amount {amount} (scaled={scaled}, decimals={colSettings.PriceDecimalPoint}) " +
                    $"не помещается в {digitsCount} цифр (max={max}).");

            string amountStr = scaled.ToString(digitsCount == 5 ? "D5" : "D6", CultureInfo.InvariantCulture);

            // 4) Собираем сообщение
            var message = new List<byte>
            {
                presetControl,          // F1 или F2
                DataControl_Level1,     // F4
                DataControl_GradeNext   // F6
            };

            // grade: 1..16 -> 0..15
            int gradeNibble = Math.Clamp(grade - 1, 0, 15);
            message.Add(BuildDataWord(gradeNibble));     // EX (E0..EF)

            message.Add(DataControl_PresetAmountNext);   // F8

            // 5) Цифры пресета: LSD-first
            message.AddRange(BuildBcdDigits(amountStr));

            return BuildDataBlock(message);
        }

        #region Helpers

        private static byte[] ReadUntilNextTag(byte[] frame, ref int index)
        {
            int start = index;

            while (index < frame.Length)
            {
                byte b = frame[index];

                if (b == 0xF0 || b == 0xFB) break;

                if (b == 0xF8 || b == 0xF6 || b == 0xF4 ||
                    b == 0xF7 || b == 0xF9 || b == 0xFA)
                    break;

                index++;
            }

            int len = index - start;
            var data = new byte[len];

            if (len > 0)
                Buffer.BlockCopy(frame, start, data, 0, len);

            return data;
        }

        private static bool IsExDigit(byte b) => b >= 0xE0 && b <= 0xE9;

        private static int ExDigit(byte b)
        {
            if (!IsExDigit(b))
                throw new FormatException($"Ожидалась EX-цифра, пришло 0x{b:X2}");

            return b - 0xE0;
        }

        private static decimal? ParseExLsdDecimal(byte[] digits, int count, int decimals)
        {
            if (digits == null || digits.Length < count)
                return null;

            long value = 0;
            long multiplier = 1;

            for (int i = 0; i < count; i++)
            {
                value += ExDigit(digits[i]) * multiplier;
                multiplier *= 10;
            }

            decimal result = value;

            for (int i = 0; i < decimals; i++)
                result /= 10m;

            return result;
        }

        /// <summary>
        /// Преобразует BCD-цифры формата LSD first в целое число.
        /// </summary>
        /// <remarks>
        /// Пример:
        /// E4 E4 → 44  
        /// E0 E0 E0 E0 → 0
        /// </remarks>
        /// <param name="data">Массив байт.</param>
        /// <param name="start">Индекс первого байта с цифрой.</param>
        /// <param name="length">Количество цифр.</param>
        /// <returns>Результирующее число.</returns>
        private static long DecodeLsdBcd(byte[] data, int start, int length)
        {
            long result = 0;
            long multiplier = 1;

            for (int i = 0; i < length; i++)
            {
                int digit = data[start + i] & 0x0F;
                if (digit > 9) digit = 0;

                result += digit * multiplier;
                multiplier *= 10;
            }

            return result;
        }

        /// <summary>
        /// Быстрый расчёт 10^n для n=0..3.
        /// </summary>
        private static int Pow10(PriceDecimalPoint n) => n switch
        {
            PriceDecimalPoint.None => 1,
            PriceDecimalPoint.One => 10,
            PriceDecimalPoint.Two => 100,
            PriceDecimalPoint.Three => 1000,
            _ => throw new ArgumentOutOfRangeException(nameof(n))
        };

        private static IEnumerable<byte> BuildBcdDigits(string digits)
        {
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                yield return BuildDataWord(digits[i] - '0');
            }
        }

        private static byte[] BuildDataBlock(IReadOnlyList<byte> messageWords)
        {
            var words = new List<byte>(messageWords.Count + 5) { StartOfText };
            var wordCount = messageWords.Count + 3; // LRCn + LRC + ETX
            words.Add(BuildDataLength(wordCount));
            words.AddRange(messageWords);
            words.Add(DataControl_LrcNext);
            var lrc = CalculateLrc(words);
            words.Add(lrc);
            words.Add(EndOfText);
            return words.ToArray();
        }

        private static byte BuildDataLength(int wordCount)
        {
            var nibble = (16 - (wordCount % 16)) % 16;
            return BuildDataWord(nibble);
        }

        private static byte BuildDataWord(int digit)
        {
            return (byte)(DataWordPrefix | (digit & 0x0F));
        }

        /// <summary>
        /// Расчет контрольной суммы LRC для блока данных [7, 8].
        /// </summary>
        /// <remarks>
        /// LRC — это дополнение до двух суммы младших нибблов всех слов в блоке [7, 8].
        /// </remarks>
        private static byte CalculateLrc(IEnumerable<byte> words)
        {
            int sum = words.Sum(word => word & 0x0F);
            int lrcNibble = (16 - (sum % 16)) % 16;
            return (byte)(0xE0 | lrcNibble); // Префикс E0 для слов данных [4, 9]
        }

        #endregion
    }
}

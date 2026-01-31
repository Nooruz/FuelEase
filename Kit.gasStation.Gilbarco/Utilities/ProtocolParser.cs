using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.HardwareConfigurations.Models;
using System.Diagnostics;
using System.Reflection.Emit;

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

        public static ControllerResponse ParseResponse(byte[] rawResponse)
        {
            try
            {
                var response = new ControllerResponse() { IsValid = false };
                if (rawResponse == null || rawResponse.Length == 0)
                    return response;

                if (rawResponse.Length == 2)
                {
                    var (controllerAddress, status) = ParseStatusAndAddress(rawResponse[1]);

                    response.Address = controllerAddress;
                    response.Status = GilbarcoStatusToNozzleStatusConverter(status);
                    response.IsLifted = status == GilbarcoStatus.Call;

                    return response;
                }

                if (rawResponse.Length == 19)
                {

                }

                return response;
            }
            catch (Exception e)
            {

                throw;
            }
        }

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
        public static byte[] PackCommand(int pumpId)
        {
            byte addr = (byte)(pumpId == 16 ? 0 : pumpId & 0x0F); // 16 кодируется как 0 [1, 4]
            byte value = (byte)(((byte)Command.Status << 4) | addr);

            return new[] { value };
        }

        /// <summary>
        /// Декодирует байт из модифицированного ASCII Gilbarco (B0-C6) в число (0-15) [5, 6].
        /// </summary>
        public static int DecodeModifiedAscii(byte b) => b >= 0xC1 ? b - 0xC1 + 10 : b - 0xB0;

        /// <summary>
        /// Кодирует число (0-15) в байт модифицированного ASCII Gilbarco (B0-C6) [5, 6].
        /// </summary>
        public static byte EncodeModifiedAscii(int value) => (byte)(value >= 10 ? 0xC1 + (value - 10) : 0xB0 + value);

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
                    break;
                case GilbarcoStatus.Busy:
                    break;
                case GilbarcoStatus.TransactionCompletePeot:
                    break;
                case GilbarcoStatus.TransactionCompleteFeot:
                    break;
                case GilbarcoStatus.PumpStop:
                    break;
                case GilbarcoStatus.SendData:
                    break;
                default:
                    break;
            }

            return NozzleStatus.Unknown;
        }

        /// <summary>
        /// Формирует байт команды Data Next (0x2) для указанного адреса ТРК [1].
        /// </summary>
        /// <param name="pumpId">Номер ТРК (1-16).</param>
        public static byte[] BuildDataNextCommand(int pumpId)
        {
            // Согласно протоколу, адрес 16 передается как 0 [1, 2].
            byte addressNibble = (byte)(pumpId == 16 ? 0 : pumpId & 0x0F);

            // Код команды (0x2) записывается в старший ниббл, адрес — в младший [1, 3].
            return new byte[] { (byte)(0x20 | addressNibble) };
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
                // Пример: price=65.50, ppuDecimals=2 => scaled=6550 => "6550"
                var scale = Pow10(settings.PriceDecimalPoint);
                var scaled = (int)Math.Round(price * scale, MidpointRounding.AwayFromZero);

                if (scaled < 0 || scaled > 9999)
                    throw new ArgumentOutOfRangeException(nameof(price),
                        $"Цена {price} с decimals={settings.PriceDecimalPoint} не помещается в 4 цифры PPU (XXXX). Получилось: {scaled}.");


                // 2) Формируем полезную нагрузку Price Change Data:
                // <pput> (F4/F5) + <gn>(F6) <g>(EX) + <ppun>(F7) <ppu>(EX EX EX EX)
                // где LSD передаётся первым. :contentReference[oaicite:2]{index=2}
                var payload = new List<byte>
                {
                    // Уровень цены: F4 для Level 1, F5 для Level 2 [1, 2]
                    DataControl_Level1,

                    // Выбор сорта: F6 (метка) + EX (номер сорта 0-F) [2]
                    0xF6,
                    (byte)(0xE0 | (column.Nozzle - 1)), // Сорта 1-16 транслируются в 0-F [2]

                    // Цена PPU: F7 (метка) + 4 цифры BCD (младшая цифра первой — LSD first) [2]
                    0xF7
                };

                // 3) XXXX в “цифрах” EX, LSD first
                // scaled=6550 -> digits "6550" -> отправка: 0,5,5,6
                string s = scaled.ToString("D4");
                payload.Add((byte)(0xE0 | (s[3] - '0'))); // LSD
                payload.Add((byte)(0xE0 | (s[2] - '0')));
                payload.Add((byte)(0xE0 | (s[1] - '0')));
                payload.Add((byte)(0xE0 | (s[0] - '0'))); // MSD

                // 2. Оборачиваем данные в стандартный блок (STX, DL, LRCn, LRC, ETX)
                return BuildDataBlock(payload);
            }
            return Array.Empty<byte>();
        }

        #region Helpers

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

        private static byte[] BuildDataNextFrame(int pumpId, byte[] dataBlock)
        {
            var frame = new byte[dataBlock.Length + 1];
            frame[0] = (byte)(0x20 | (pumpId & 0x0F));
            Buffer.BlockCopy(dataBlock, 0, frame, 1, dataBlock.Length);
            return frame;
        }

        private static byte[] BuildPresetBlock(int pumpId, int grade, decimal amount, bool bySum)
        {
            var presetControl = bySum ? DataControl_MoneyPreset : DataControl_VolumePreset;
            var presetAmount = Math.Clamp((int)Math.Round(amount * 100m, MidpointRounding.AwayFromZero), 0, 99999);
            var amountStr = presetAmount.ToString("D5");
            var message = new List<byte> { presetControl, DataControl_Level1 };

            if (!bySum)
            {
                var gradeWord = BuildDataWord(Math.Clamp(grade - 1, 0, 15));
                message.Add(DataControl_GradeNext);
                message.Add(gradeWord);
            }

            message.Add(DataControl_PresetAmountNext);
            message.AddRange(BuildBcdDigits(amountStr));

            return BuildDataBlock(message);
        }

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

        private static byte[] BuildRawDataBlock(IReadOnlyList<byte> messageWords)
        {
            var words = new List<byte>(messageWords.Count + 2)
            {
                StartOfText // 0xFF
            };

            words.AddRange(messageWords);
            words.Add(EndOfText); // 0xF0
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

        private static (byte controllerAddress, GilbarcoStatus status) ParseStatusAndAddress(byte statusByte)
        {
            // Адрес пистолета — в старших 4 битах
            byte controllerAddress = (byte)(statusByte & 0x0F);

            // Код статуса — в младших 4 битах
            byte statusCode = (byte)((statusByte >> 4) & 0x0F);

            GilbarcoStatus status = statusCode switch
            {
                6 => GilbarcoStatus.Off,
                7 => GilbarcoStatus.Call,
                _ => GilbarcoStatus.DataError
            };

            return (controllerAddress, status);
        }

        private static void ParseExtendedStatus010(byte[] rx, ControllerResponse response)
        {
            //// BA - признак начала блока данных Special Function [4]
            //if (rx == null || rx.Length < 19 || rx != 0xBA) return;

            //// Вспомогательная функция для расшифровки нибблов (B0->0, C1->10 и т.д.) [3, 5]
            //int Decode(byte b) => b >= 0xC1 ? b - 0xC1 + 10 : b - 0xB0;

            //// Поля сообщения 'm' начинаются с 10-го байта в данном формате [4, 6]
            //response.PriceLevelNeeded = Decode(rx[7]) == 0; // 0 = Needed, 1 = Not Needed [6]
            //response.GradeSelectionNeeded = Decode(rx[8]) == 0;
            //response.IsNozzleOut = Decode(rx[9]) == 1;      // 0 = Off/In, 1 = On/Out [6]
            //response.PushToStartNeeded = Decode(rx[10]) == 0;
            //response.SelectedGrade = Decode(rx[11]);         // Номер выбранного сорта [6]

            //response.IsValid = true;
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

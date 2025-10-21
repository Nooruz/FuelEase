using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.FuelDispenser.Services
{
    public class PKElectronicsProtocolParser : IProtocolParser
    {
        #region Private Members

        private readonly ICommandEncoder _commandEncoder;

        #endregion

        #region Constructors

        public PKElectronicsProtocolParser(ICommandEncoder commandEncoder)
        {
            _commandEncoder = commandEncoder;
        }

        #endregion

        #region Public Voids

        public byte[] BuildRequest(Command cmd, int controllerAddress, int columnAddress, decimal? value = null, decimal? quantity = null)
        {
            // Формируем байт адреса: старшие 4 бита - controller, младшие - nozzle
            byte addr = (byte)(((controllerAddress & 0x0F) << 4) | (columnAddress & 0x0F));

            var list = new List<byte> { _commandEncoder.Encode(cmd), addr };

            switch (cmd)
            {
                case Command.Status:
                    break;
                case Command.StartFillingSum:
                    if (value != null && quantity != null)
                    {
                        list.AddRange(ConvertQuantityAndSumToBytes(quantity.Value, value.Value));
                    }
                    break;
                case Command.StartFillingQuantity:
                    break;
                case Command.StopFilling:
                    break;
                case Command.CompleteFilling:
                    break;
                case Command.ContinueFilling:
                    break;
                case Command.ChangePrice:
                    if (value != null)
                    {
                        list.AddRange(ConvertDecimalToBytes(value.Value));
                    }
                    break;
                case Command.CounterLiter:
                    break;
                case Command.CounterSum:
                    break;
                case Command.FirmwareVersion:
                    break;
                case Command.ProgramControlMode:
                    break;
                case Command.KeyboardControlMode:
                    break;
                case Command.Sensor:
                    list[0] = (byte)(controllerAddress << 4);
                    list[1] = GetNozzleSensorBytes();
                    break;
                case Command.ReduceCosts:
                    break;
                case Command.PumpAccelerationTime:
                    break;
                default:
                    break;
            }

            list.Add(CheckSum(list.ToArray()));

            return list.ToArray();
        }

        public ControllerResponse ParseResponse(byte[] rawResponse)
        {
            // Проверяем контрольную сумму
            var checksum = CheckSum(rawResponse, rawResponse.Length - 1);
            if (checksum != rawResponse[^1])
                return new ControllerResponse { IsValid = false };

            var receivedcmd = _commandEncoder.Decode(rawResponse[0]);
            var columnAddress = rawResponse[1] & 0x0F;

            //var status = ConvertByteToNozzleStatus(rawResponse[2]);

            var lifted = rawResponse[2];

            if (receivedcmd == Command.Screen)
            {

            }

            return new ControllerResponse
            {
                IsValid = true,
                Command = receivedcmd,
                //Status = status,
                Address = columnAddress,
                IsLifted = lifted is 0x93 or 0x83
            };
        }

        private byte GetNozzleSensorBytes()
        {
            try
            {
                // Предполагаем, что NormallyOpen имеет значение 0
                const int NormallyOpen = 0;

                //// Собираем значения NozzleSensor из первых четырех элементов коллекции Nozzles
                ////var sensorValues = Nozzles.Take(4).Select(n => n.NozzleSensor == NormallyOpen ? 0 : 1).ToArray();

                ////// Преобразуем значения в двоичное представление
                //string binaryString = string.Join("", sensorValues);

                ////// Преобразуем двоичное представление в шестнадцатеричное
                //int hexValue = Convert.ToInt32(binaryString, 2);
                //byte hexByte = (byte)hexValue;

                return 0x00;
            }
            catch (Exception)
            {
                //ignore
            }

            return 0x00;
        }

        #endregion

        #region Private Members

        /// <summary>
        /// Вычисляет контрольную сумму для массива байтов.
        /// </summary>
        /// <param name="bytes">Массив байтов для вычисления.</param>
        /// <returns>Массив байтов с добавленной контрольной суммой.</returns>
        private byte CheckSum(byte[] bytes, int? length = null)
        {
            int count = length ?? bytes.Length;

            if (count > bytes.Length || count < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Specified length is out of range.");

            // Вычисление контрольной суммы XOR по указанному количеству байт
            byte checksum = bytes.Take(count).Aggregate((byte)0, (current, next) => (byte)(current ^ next));

            return checksum;
        }

        //private NozzleStatus ConvertByteToNozzleStatus(byte statusByte)
        //{
        //    return statusByte switch
        //    {
        //        0x91 or 0x81 => NozzleStatus.Ready,
        //        0x93 or 0x83 => NozzleStatus.Ready,
        //        0x96 or 0x86 => NozzleStatus.WaitingStop,
        //        0xA6 or 0xB6 => NozzleStatus.PumpWorking,
        //        0x9B or 0x8B or 0x9E or 0x8E => NozzleStatus.Blocking,
        //        _ => NozzleStatus.Unknown
        //    };
        //}

        private byte[] ConvertDecimalToBytes(decimal decValue)
        {
            int amountInTyins = (int)(decValue * 100); // Преобразуем в тыйины (целое число)

            byte[] byteArray = BitConverter.GetBytes(amountInTyins);

            // Порядок байтов может быть важен (LittleEndian/BigEndian), для корректного отображения:
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(byteArray); // Переворачиваем для BigEndian порядка
            }

            return byteArray = byteArray.SkipWhile(b => b == 0).ToArray();
        }

        private byte[] ConvertQuantityAndSumToBytes(decimal quantity, decimal sum)
        {
            // Преобразуем quantity в целое число
            int quantityInt = (int)(quantity * 1000); // Умножаем на 100 для сохранения двух знаков после запятой
            byte[] quantityBytes = BitConverter.GetBytes(quantityInt);

            // Преобразуем sum в целое число
            int sumInt = (int)(sum * 100); // Умножаем на 100 для сохранения двух знаков после запятой
            byte[] sumBytes = BitConverter.GetBytes(sumInt);

            // Порядок байтов может быть важен (LittleEndian/BigEndian), для корректного отображения:
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(quantityBytes); // Переворачиваем для BigEndian порядка
                Array.Reverse(sumBytes); // Переворачиваем для BigEndian порядка
            }

            // Возьмем последние 3 байта для quantity и sum
            byte[] result = new byte[6];
            Array.Copy(quantityBytes, 1, result, 0, 3); // Берем байты с 1 по 3 (всего 3 байта)
            Array.Copy(sumBytes, 1, result, 3, 3); // Берем байты с 1 по 3 (всего 3 байта)

            return result;
        }

        #endregion
    }
}

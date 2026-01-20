using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.FuelDispenser.Services
{
    public class GilbarcoProtocolParser
    {
        private const byte StartOfText = 0xFF;
        private const byte EndOfText = 0xF0;
        private const byte DataControl_VolumePreset = 0xF1;
        private const byte DataControl_MoneyPreset = 0xF2;
        private const byte DataControl_Level1 = 0xF4;
        private const byte DataControl_Level2 = 0xF5;
        private const byte DataControl_GradeNext = 0xF6;
        private const byte DataControl_PpuNext = 0xF7;
        private const byte DataControl_PresetAmountNext = 0xF8;
        private const byte DataControl_LrcNext = 0xFB;
        private const byte DataWordPrefix = 0xE0;

        private readonly ICommandEncoder _encoder;

        public GilbarcoProtocolParser(ICommandEncoder encoder)
        {
            _encoder = encoder;
        }

        public ControllerResponse ParseResponse(byte[] rawResponse)
        {
            if (rawResponse == null || rawResponse.Length == 0)
                return new() { IsValid = false };

            byte status = (byte)(rawResponse[0] & 0xF0);
            byte pumpId = (byte)(rawResponse[0] & 0x0F);

            NozzleStatus nozzleStatus = status switch
            {
                //0x60 => NozzleStatus.Off,
                //0x70 => NozzleStatus.Call,
                0x80 => NozzleStatus.Ready,
                0x90 => NozzleStatus.PumpWorking,
                0xA0 => NozzleStatus.PumpStop, // PEOT
                0xB0 => NozzleStatus.PumpStop, // FEOT
                0xC0 => NozzleStatus.WaitingStop,
                0x00 => NozzleStatus.Unknown,  // Data Error
                _ => NozzleStatus.Unknown
            };

            return new ControllerResponse
            {
                Address = pumpId,
                Command = _encoder.Decode((byte)(status >> 4)),
                Data = rawResponse,
                IsValid = true,
                Status = nozzleStatus,
                StatusAddress = pumpId
            };
        }

        public byte[] BuildRequest(Command cmd, 
            int controllerAddress, 
            int columnAddress, 
            decimal? value = null, 
            bool bySum = true, 
            LanfengControllerType controllerType = LanfengControllerType.None)
        {
            if (cmd == Command.Status)
            {
                return new byte[] { (byte)(0x00 | (controllerAddress & 0x0F)) };
            }
            //else if (cmd == Command.Authorize)
            //{
            //    return new byte[] { (byte)(0x10 | (controllerAddress & 0x0F)) };
            //}
            else if (cmd == Command.StopFueling)
            {
                return new byte[] { (byte)(0x30 | (controllerAddress & 0x0F)) };
            }
            else if (cmd == Command.CounterSum || cmd == Command.CounterLiter)
            {
                return new byte[] { (byte)(0x50 | (controllerAddress & 0x0F)) };
            }
            else if (cmd == Command.ChangePrice)
            {
                var dataBlock = BuildPriceChangeBlock(controllerAddress, columnAddress, value ?? 0m);
                return BuildDataNextFrame(controllerAddress, dataBlock);
            }
            else if (cmd == Command.SendData)
            {
                if (!value.HasValue)
                {
                    throw new ArgumentException("Preset amount is required for SendData command.", nameof(value));
                }

                var dataBlock = BuildPresetBlock(controllerAddress, columnAddress, value.Value, bySum);
                return BuildDataNextFrame(controllerAddress, dataBlock);
            }

            throw new NotSupportedException($"Command {cmd} not implemented for Gilbarco.");
        }

        #region Helpers

        private static byte[] BuildDataNextFrame(int pumpId, byte[] dataBlock)
        {
            var frame = new byte[dataBlock.Length + 1];
            frame[0] = (byte)(0x20 | (pumpId & 0x0F));
            Buffer.BlockCopy(dataBlock, 0, frame, 1, dataBlock.Length);
            return frame;
        }

        private byte[] BuildPriceChangeBlock(int pumpId, int grade, decimal price)
        {
            // Цена в формате XXXX (BCD, 4 цифры, LSD first)
            int priceInt = (int)(price * 100); // 123.45 → 12345 → но у Gilbarco только 4 цифры!
            if (priceInt > 9999) priceInt = 9999;

            string priceStr = priceInt.ToString("D4");
            var gradeWord = BuildDataWord(Math.Clamp(grade - 1, 0, 15));
            var message = new List<byte>
            {
                DataControl_Level1,
                DataControl_GradeNext,
                gradeWord,
                DataControl_PpuNext,
            };
            message.AddRange(BuildBcdDigits(priceStr));
            return BuildDataBlock(message);
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

        private static byte BuildDataLength(int wordCount)
        {
            var nibble = (16 - (wordCount % 16)) % 16;
            return BuildDataWord(nibble);
        }

        private static byte BuildDataWord(int digit)
        {
            return (byte)(DataWordPrefix | (digit & 0x0F));
        }

        private static byte CalculateLrc(IEnumerable<byte> words)
        {
            var sum = words.Sum(word => word & 0x0F);
            var lrcNibble = (16 - (sum % 16)) % 16;
            return BuildDataWord(lrcNibble);
        }

        #endregion
    }
}

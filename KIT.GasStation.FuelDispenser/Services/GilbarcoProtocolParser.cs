using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KIT.GasStation.FuelDispenser.Services
{
    public class GilbarcoProtocolParser : IProtocolParser
    {
        private const byte StartOfText = 0xFF;
        private const byte EndOfText = 0xF0;
        private const byte DataControl_PresetAmount = 0xF8;
        private const byte DataControl_Grade = 0xF6;
        private const byte DataControl_PPU = 0xF7;
        private const byte DataWordPrefix = 0xE0;
        private const byte DataControlWordPrefix = 0xF0;

        private readonly ICommandEncoder _encoder;

        public GilbarcoProtocolParser(ICommandEncoder encoder)
        {
            _encoder = encoder;
        }

        // Простой запрос статуса: [0x0][pumpId]
        public byte[] BuildRequest(Command cmd, int controllerAddress, int columnAddress = 0, decimal? value = null, bool bySum = true)
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
                return BuildPriceChangeBlock(controllerAddress, columnAddress, value ?? 0m);
            }

            throw new NotSupportedException($"Command {cmd} not implemented for Gilbarco.");
        }

        private byte[] BuildPriceChangeBlock(int pumpId, int grade, decimal price)
        {
            // Цена в формате XXXX (BCD, 4 цифры, LSD first)
            int priceInt = (int)(price * 100); // 123.45 → 12345 → но у Gilbarco только 4 цифры!
            if (priceInt > 9999) priceInt = 9999;

            string priceStr = priceInt.ToString("D4"); // "1234"

            var block = new List<byte>
            {
                StartOfText,
                0xE5, // DL = 5 (фиксировано)
                0xF4, // Level 1 Price
                0xF6, // Grade next
                (byte)(0xE0 | (grade & 0x0F)),
                0xF7, // PPU next
                (byte)(0xE0 | (priceStr[3] - '0')), // LSD
                (byte)(0xE0 | (priceStr[2] - '0')),
                (byte)(0xE0 | (priceStr[1] - '0')),
                (byte)(0xE0 | (priceStr[0] - '0')), // MSD
                0xFB, // LRC next
            };

            //byte lrc = CalculateLrc(block.Skip(1).Take(block.Count - 1 - 1)); // без STX и LRCn
            //block.Add(lrc);
            //block.Add(EndOfText);

            // Оборачиваем в Data Next + Pump ID
            var full = new List<byte>
            {
                //0x20 | (pumpId & 0x0F) // Data Next команда
            };
            full.AddRange(block);

            return full.ToArray();
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

        // Helper: парсинг BCD-полей вида E1 E2 → "12"
        private static decimal ParseGilbarcoBcdField(ReadOnlySpan<byte> bytes, int decimalPlaces = 0)
        {
            var digits = new List<char>();
            foreach (byte b in bytes)
            {
                if ((b & 0xF0) != 0xE0)
                    throw new FormatException("Invalid Data Word prefix");
                char digit = (char)('0' + (b & 0x0F));
                digits.Add(digit);
            }
            string numStr = string.Concat(digits);
            if (decimalPlaces > 0 && numStr.Length > decimalPlaces)
            {
                numStr = numStr.Insert(numStr.Length - decimalPlaces, ".");
            }
            return decimal.Parse(numStr);
        }

        public byte[] BuildRequest(Command cmd, int controllerAddress, int columnAddress, decimal? value = null, bool bySum = true, LanfengControllerType controllerType = LanfengControllerType.None)
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
                return BuildPriceChangeBlock(controllerAddress, columnAddress, value ?? 0m);
            }

            throw new NotSupportedException($"Command {cmd} not implemented for Gilbarco.");
        }
    }
}

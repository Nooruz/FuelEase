using FuelEase.Domain.Models;
using FuelEase.FuelDispenser.Commands;

namespace FuelEase.FuelDispenser.Models
{
    /// <summary>
    /// DTO, представляющий проанализированный ответ устройства.
    /// </summary>
    public class DeviceResponse
    {
        public int Address { get; set; }
        public int StatusAddress { get; set; }
        public Command Command { get; set; }
        public byte[] Data { get; set; }
        public bool IsValid { get; set; }
        public decimal Quantity { get; set; }
        public decimal Sum { get; set; }
        public bool IsLifted { get; set; }

        /// <summary>
        /// Статус колонки, извлечённый из 12-го байта ответа.
        /// </summary>
        public NozzleStatus Status { get; set; }
    }
}

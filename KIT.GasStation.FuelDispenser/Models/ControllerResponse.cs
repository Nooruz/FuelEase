using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Commands;

namespace KIT.GasStation.FuelDispenser.Models
{
    /// <summary>
    /// DTO, представляющий проанализированный ответ устройства.
    /// </summary>
    public class ControllerResponse
    {
        public int Address { get; set; }
        public int StatusAddress { get; set; }
        public Command Command { get; set; }
        public byte[] Data { get; set; }
        public bool IsValid { get; set; }
        public decimal Quantity { get; set; }
        public decimal Sum { get; set; }
        public bool IsLifted { get; set; }
        public string Group { get; set; }
        public NozzleStatus Status { get; set; }
    }
}

using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.Lanfeng.Utilities
{
    public class LanfengResonse
    {
        public int Address { get; set; }
        public int? StatusAddress { get; set; }
        public Command Command { get; set; }
        public byte[] Data { get; set; }
        public bool IsValid { get; set; }
        public decimal Quantity { get; set; }
        public decimal Sum { get; set; }
        public bool IsLifted { get; set; }
        public string Group { get; set; }
        public NozzleStatus Status { get; set; }
        public decimal CounterQuantity { get; set; }
    }
}

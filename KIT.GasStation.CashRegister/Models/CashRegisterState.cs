using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.CashRegisters.Models
{
    public class CashRegisterState
    {
        private DateTime? _openedAt;
        public DateTime? OpenedAt
        {
            get => _openedAt;
            set
            {
                _openedAt = value;

                if (_openedAt == null) return;

                var elapsed = DateTime.Now - _openedAt.Value;

                if (elapsed.TotalHours > 24)
                {
                    Status = CashRegisterStatus.Exceeded24Hours;
                }
                else
                {
                    Status = CashRegisterStatus.Open;
                }
            }
        }
        public int ShiftNumber { get; set; }
        public CashRegisterStatus Status { get; set; } = CashRegisterStatus.Unknown;
    }
}

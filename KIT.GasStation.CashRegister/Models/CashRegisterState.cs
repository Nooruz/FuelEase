using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.CashRegisters.Models
{
    public class CashRegisterState
    {
        public DateTime? OpenedAt { get; set; }

        public CashRegisterStatus Status
        {
            get
            {
                if (OpenedAt == null)
                {
                    return CashRegisterStatus.Unknown;
                }
                var elapsed = DateTime.Now - OpenedAt.Value;

                if (elapsed.TotalHours > 24)
                {
                    return CashRegisterStatus.Exceeded24Hours;
                }

                return CashRegisterStatus.Open;
            }
        }
    }
}

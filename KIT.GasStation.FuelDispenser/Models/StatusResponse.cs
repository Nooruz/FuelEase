using KIT.GasStation.Domain.Models;

namespace KIT.GasStation.FuelDispenser.Models
{
    public class StatusResponse : Response
    {
        public NozzleStatus Status { get; set; }
    }
}

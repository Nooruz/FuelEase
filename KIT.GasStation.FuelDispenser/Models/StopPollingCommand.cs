namespace KIT.GasStation.FuelDispenser.Models
{
    public sealed class StopPollingCommand
    {
        public Guid CommandId { get; set; }
        public string GroupName { get; set; } = "";
    }
}

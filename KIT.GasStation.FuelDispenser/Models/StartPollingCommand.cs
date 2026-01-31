namespace KIT.GasStation.FuelDispenser.Models
{
    public sealed class StartPollingCommand
    {
        public Guid CommandId { get; set; }
        public string GroupName { get; set; } = "";
    }
}

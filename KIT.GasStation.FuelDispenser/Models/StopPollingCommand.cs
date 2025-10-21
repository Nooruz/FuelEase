namespace KIT.GasStation.FuelDispenser.Models
{
    public sealed class StopPollingCommand
    {
        public string ControllerKey { get; set; } = "";
        public string ControllerName { get; set; }
        public string ColumnName { get; set; }
    }
}

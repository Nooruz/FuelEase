namespace KIT.GasStation.FuelDispenser.Models
{
    public sealed record CommandCompletion
    {
        public Guid CommandId { get; init; }
        public string GroupName { get; init; } = string.Empty;
        public bool IsSuccess { get; init; } = true;
        public string? ErrorMessage { get; init; }
    }
}

namespace KIT.GasStation.FuelDispenser.Models
{
    /// <summary>
    /// Сообщение от воркера о текущей доступности оборудования.
    /// </summary>
    public sealed record WorkerAvailabilityReport
    {
        public string GroupName { get; init; } = string.Empty;
        public bool IsAvailable { get; init; }
        public string? Reason { get; init; }
    }
}

namespace KIT.GasStation.FuelDispenser.Models
{
    /// <summary>
    /// Снимок состояния воркера для конкретной группы.
    /// </summary>
    public sealed record WorkerStateNotification
    {
        public string GroupName { get; init; } = string.Empty;
        public bool IsOnline { get; init; }
        public string? Reason { get; init; }
        public DateTime ChangedAt { get; init; } = DateTime.Now;
    }
}

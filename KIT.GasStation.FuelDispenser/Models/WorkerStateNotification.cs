using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;
    }
}

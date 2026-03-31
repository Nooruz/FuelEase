using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.Web.Services;
using System.Collections.Concurrent;

namespace KIT.GasStation.Worker.Services
{
    public sealed class WorkerStateStore : IWorkerStateStore
    {
        private readonly ConcurrentDictionary<string, WorkerStateNotification> _states = new();

        public bool TryGet(string groupName, out WorkerStateNotification notification) =>
            _states.TryGetValue(groupName, out notification!);

        public IReadOnlyCollection<WorkerStateNotification> GetSnapshot(IEnumerable<string>? groupNames = null)
        {
            if (groupNames is null)
            {
                return _states.Values.ToArray();
            }

            var allowed = new HashSet<string>(groupNames.Where(g => !string.IsNullOrWhiteSpace(g)), StringComparer.Ordinal);
            if (allowed.Count == 0)
                return Array.Empty<WorkerStateNotification>();

            return _states
                .Where(pair => allowed.Contains(pair.Key))
                .Select(pair => pair.Value)
                .ToArray();
        }

        public bool TryUpdate(string groupName, bool isOnline, string? reason, out WorkerStateNotification notification)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new ArgumentException("Имя группы не может быть пустым", nameof(groupName));

            var sanitizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            while (true)
            {
                if (_states.TryGetValue(groupName, out var current))
                {
                    if (current.IsOnline == isOnline && string.Equals(current.Reason ?? string.Empty, sanitizedReason ?? string.Empty, StringComparison.Ordinal))
                    {
                        notification = current;
                        return false;
                    }

                    var updated = current with
                    {
                        IsOnline = isOnline,
                        Reason = sanitizedReason,
                        ChangedAt = DateTime.Now
                    };

                    if (_states.TryUpdate(groupName, updated, current))
                    {
                        notification = updated;
                        return true;
                    }

                    continue;
                }

                var created = new WorkerStateNotification
                {
                    GroupName = groupName,
                    IsOnline = isOnline,
                    Reason = sanitizedReason,
                    ChangedAt = DateTime.Now
                };

                if (_states.TryAdd(groupName, created))
                {
                    notification = created;
                    return true;
                }
            }
        }
    }
}

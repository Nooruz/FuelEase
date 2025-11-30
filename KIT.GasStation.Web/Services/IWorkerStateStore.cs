using KIT.GasStation.FuelDispenser.Models;

namespace KIT.GasStation.Web.Services
{
    public interface IWorkerStateStore
    {
        bool TryGet(string groupName, out WorkerStateNotification notification);
        IReadOnlyCollection<WorkerStateNotification> GetSnapshot(IEnumerable<string>? groupNames = null);
        bool TryUpdate(string groupName, bool isOnline, string? reason, out WorkerStateNotification notification);
    }
}

using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.Web.Services;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace KIT.GasStation.Web.Hubs
{
    public sealed class DeviceResponseHub : Hub<IDeviceResponseClient>
    {
        #region Private Members

        private readonly IGroupRegistry _groups;
        private readonly ILogger<DeviceResponseHub> _log;
        private readonly IWorkerStateStore _workerStateStore;
        private static readonly ConcurrentDictionary<string, byte> _workerConnections = new();

        #endregion

        #region Constructors

        public DeviceResponseHub(IGroupRegistry groups, IWorkerStateStore workerStateStore, ILogger<DeviceResponseHub> log)
        {
            _groups = groups;
            _workerStateStore = workerStateStore;
            _log = log;
        }

        #endregion

        #region Public Voids

        public async Task JoinController(string groupName, bool isWorker = false)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _groups.Add(Context.ConnectionId, groupName);
            _log.LogInformation("JOIN {Conn} -> {Group}. Worker={Worker}", Context.ConnectionId, groupName, isWorker);

            if (isWorker)
            {
                _workerConnections.TryAdd(Context.ConnectionId, 0);
                await BroadcastWorkerStateChangedAsync(groupName, true, "Worker connected");
            }
            else if (_workerStateStore.TryGet(groupName, out var state))
            {
                await Clients.Caller.WorkerStateChanged(state);
            }
        }

        public async Task LeaveController(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _groups.Remove(Context.ConnectionId, groupName);
            _log.LogInformation("LEAVE {Conn} -> {Group}", Context.ConnectionId, groupName);
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var groups = _groups.GetGroupsForConnection(Context.ConnectionId);
            _groups.RemoveAllForConnection(Context.ConnectionId);

            if (_workerConnections.TryRemove(Context.ConnectionId, out _))
            {
                foreach (var g in groups)
                {
                    await BroadcastWorkerStateChangedAsync(g, false, "SignalR disconnected");
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // Группы текущего подключения
        public Task<IReadOnlyCollection<string>> GetMyGroups() =>
            Task.FromResult(_groups.GetGroupsForConnection(Context.ConnectionId));

        // Все группы (на этом серверном узле)
        public Task<IReadOnlyCollection<string>> GetAllGroups() =>
            Task.FromResult(_groups.GetAllGroups());

        public Task SetPriceAsync(string groupName, decimal price) =>
            Clients.Group(groupName).SetPriceAsync(groupName, price);

        public Task StartRefuelingAsync(string groupName, decimal sum, bool bySum) =>
            Clients.Group(groupName).StartRefuelingAsync(groupName, sum, bySum);
        public Task ColumnLiftedChanged(string groupName, bool isLifted) =>
            Clients.Group(groupName).ColumnLiftedChanged(groupName, isLifted);
        public Task CompleteRefuelingAsync(string groupName) =>
            Clients.Group(groupName).CompleteRefuelingAsync(groupName);
        public Task GetCountersAsync(string groupName) =>
            Clients.Group(groupName).GetCountersAsync(groupName);

        public Task ChangeControlModeAsync(string groupName, bool isProgramMode) =>
            Clients.Group(groupName).ChangeControlModeAsync(groupName, isProgramMode);

        public Task RegisterWorker(string groupName) =>
            Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        public Task StartPolling(string groupName) =>
            Clients.Group(groupName).StartPolling(new StartPollingCommand { GroupName = groupName });

        public Task StopPolling(string groupName) =>
            Clients.Group(groupName).StopPolling(new StopPollingCommand { GroupName = groupName });

        public async Task ReportWorkerAvailability(WorkerAvailabilityReport report)
        {
            if (report is null || string.IsNullOrWhiteSpace(report.GroupName))
                throw new HubException("Отсутствует название группы для отчета о состоянии воркера");

            var groups = _groups.GetGroupsForConnection(Context.ConnectionId);
            if (!groups.Contains(report.GroupName))
            {
                _log.LogWarning("Connection {Conn} tried to report state for unjoined group {Group}",
                    Context.ConnectionId, report.GroupName);
                throw new HubException("Подключение не зарегистрировано в указанной группе");
            }

            await BroadcastWorkerStateChangedAsync(report.GroupName, report.IsAvailable, report.Reason);
        }

        public Task<IReadOnlyCollection<WorkerStateNotification>> GetWorkerStatesSnapshot(string[]? groupNames = null)
        {
            var snapshot = _workerStateStore.GetSnapshot(groupNames);
            return Task.FromResult(snapshot);
        }


        public Task StartFilling() => Task.CompletedTask;
        public Task StopFilling() => Task.CompletedTask;

        public sealed record StatusChangedEvent(ControllerResponse response);

        public async Task PublishStatus(ControllerResponse e, string groupName)
        {
            var g = groupName;
            _last[g] = e;
            await Clients.Group(g).StatusChanged(e);
        }

        #endregion

        private static readonly ConcurrentDictionary<string, ControllerResponse> _last = new();

        private Task BroadcastWorkerStateChangedAsync(string groupName, bool isOnline, string? reason = null)
        {
            if (!_workerStateStore.TryUpdate(groupName, isOnline, reason, out var notification))
                return Task.CompletedTask;

            return Clients.Group(groupName).WorkerStateChanged(notification);
        }

    }
}

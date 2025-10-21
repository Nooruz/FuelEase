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

        #endregion

        #region Constructors

        public DeviceResponseHub(IGroupRegistry groups, ILogger<DeviceResponseHub> log)
        {
            _groups = groups;
            _log = log;
        }

        #endregion

        #region Public Voids

        public async Task JoinController(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _groups.Add(Context.ConnectionId, groupName);
            _log.LogInformation("JOIN {Conn} -> {Group}", Context.ConnectionId, groupName);
        }

        public async Task LeaveController(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _groups.Remove(Context.ConnectionId, groupName);
            _log.LogInformation("LEAVE {Conn} -> {Group}", Context.ConnectionId, groupName);
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _groups.RemoveAllForConnection(Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        // Группы текущего подключения
        public Task<IReadOnlyCollection<string>> GetMyGroups() =>
            Task.FromResult(_groups.GetGroupsForConnection(Context.ConnectionId));

        // Все группы (на этом серверном узле)
        public Task<IReadOnlyCollection<string>> GetAllGroups() =>
            Task.FromResult(_groups.GetAllGroups());

        // Пример команд от РМК к серверу:
        public Task SetPrice() =>
            // здесь пробрасываешь в свой командный обработчик/шину
            Task.CompletedTask;

        public Task RegisterWorker(string groupName) =>
            Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        public Task StartPolling(string groupName) =>
            Clients.Group(groupName).StartPolling(new StartPollingCommand { GroupName = groupName });

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

        
    }
}

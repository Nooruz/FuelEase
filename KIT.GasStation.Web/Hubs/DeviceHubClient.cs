using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.Web.Services;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace KIT.GasStation.Worker.Hubs
{
    public sealed class DeviceHubClient : Hub<IDeviceHubClient>
    {
        #region Private Members

        private readonly IGroupRegistry _groups;
        private readonly ILogger<DeviceHubClient> _log;
        private readonly IWorkerStateStore _workerStateStore;
        private static readonly ConcurrentDictionary<string, byte> _workerConnections = new();
        private static readonly ConcurrentDictionary<Guid, TaskCompletionSource<CommandCompletion>> _pendingCommands = new();
        private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(15);
        private static readonly ConcurrentDictionary<string, string> _groupWorkers = new(StringComparer.Ordinal);

        #endregion

        #region Constructors

        public DeviceHubClient(IGroupRegistry groups,
            IWorkerStateStore workerStateStore,
            ILogger<DeviceHubClient> log)
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
                _groupWorkers[groupName] = Context.ConnectionId;
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
            if (_groupWorkers.TryGetValue(groupName, out var workerConnId) && workerConnId == Context.ConnectionId)
            {
                _groupWorkers.TryRemove(groupName, out _);
                await BroadcastWorkerStateChangedAsync(groupName, false, "Worker left group");
            }
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
                    if (_groupWorkers.TryGetValue(g, out var workerConnId) && workerConnId == Context.ConnectionId)
                    {
                        _groupWorkers.TryRemove(g, out _);
                    }

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

        public async Task SetPricesAsync(IReadOnlyCollection<PriceRequest> prices)
        {
            if (prices == null || prices.Count == 0)
                return;

            var firstGroupName = prices.First().GroupName;

            await ExecuteCommandAsync(firstGroupName, commandId =>
                    GetWorkerClient(firstGroupName).SetPricesAsync(commandId, prices));
        }

        public Task SetPriceAsync(PriceRequest price)
        {
            return ExecuteCommandAsync(price.GroupName, commandId =>
                GetWorkerClient(price.GroupName).SetPriceAsync(commandId, price));
        }

        public Task StartFuelingAsync(FuelingRequest fuelingRequest) =>
            GetWorkerClient(fuelingRequest.GroupName).StartFuelingAsync(fuelingRequest);

        public Task StopFuelingAsync(string groupName) =>
            Clients.Group(groupName).StopFuelingAsync(groupName);

        public Task ResumeFuelingAsync(ResumeFuelingRequest resumeFuelingRequest) =>
            Clients.Group(resumeFuelingRequest.GroupName).ResumeFuelingAsync(resumeFuelingRequest);

        public Task GetStatusByAddressAsync(string groupName) =>
            Clients.Group(groupName).GetStatusByAddressAsync(groupName);

        public Task ColumnLiftedChanged(string groupName, bool isLifted) =>
            Clients.Group(groupName).ColumnLiftedChanged(groupName, isLifted);

        public Task CountersUpdated(string groupName, IReadOnlyCollection<CounterData> counterDatas) =>
            Clients.Group(groupName).CountersUpdated(groupName, counterDatas);

        public Task CounterUpdated(CounterData counterData) =>
            Clients.Group(counterData.GroupName).CounterUpdated(counterData);

        public Task FuelingAsync(FuelingResponse response) =>
            Clients.Group(response.GroupName).FuelingAsync(response);

        public Task CompletedFuelingAsync(string groupName, decimal? quantity) =>
            Clients.Group(groupName).CompletedFuelingAsync(groupName, quantity);

        public Task WaitingAsync(string groupName) =>
            Clients.Group(groupName).WaitingAsync(groupName);

        public Task PumpStopAsync(FuelingResponse response) =>
            Clients.Group(response.GroupName).PumpStopAsync(response);

        public Task CompleteFuelingAsync(string groupName) =>
            Clients.Group(groupName).CompleteFuelingAsync(groupName);

        public Task GetCounterAsync(string groupName) =>
            ExecuteCommandAsync(groupName, commandId =>
                Clients.Group(groupName).GetCounterAsync(commandId, groupName));

        public Task GetCountersAsync(string groupName) =>
            ExecuteCommandAsync(groupName, commandId =>
                Clients.Group(groupName).GetCountersAsync(commandId, groupName));

        public Task ChangeControlModeAsync(string groupName, bool isProgramMode) =>
            ExecuteCommandAsync(groupName, commandId =>
                Clients.Group(groupName).ChangeControlModeAsync(commandId, groupName, isProgramMode));

        public Task InitializeConfigurationAsync(string groupName) =>
            ExecuteCommandAsync(groupName, commandId =>
                Clients.Group(groupName).InitializeConfigurationAsync(commandId, groupName));

        public Task RegisterWorker(string groupName) =>
            Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        //public async Task StartPolling(string groupName)
        //{
        //    _log.LogInformation("Start polling {groupName}", groupName);
        //    await ExecuteCommandAsync(groupName, commandId =>
        //        Clients.Group(groupName).StartPolling(new StartPollingCommand
        //        {
        //            GroupName = groupName,
        //            CommandId = commandId
        //        }));
        //}

        //public async Task StopPolling(string groupName)
        //{
        //    _log.LogInformation("Stop polling {groupName}", groupName);
        //    await ExecuteCommandAsync(groupName, commandId =>
        //        Clients.Group(groupName).StopPolling(new StopPollingCommand
        //        {
        //            GroupName = groupName,
        //            CommandId = commandId
        //        }));
        //}

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

        public Task ReportCommandCompleted(CommandCompletion completion)
        {
            if (completion is null)
                return Task.CompletedTask;

            if (_pendingCommands.TryGetValue(completion.CommandId, out var tcs))
            {
                tcs.TrySetResult(completion);
            }

            return Task.CompletedTask;
        }

        public async Task PublishStatus(StatusResponse e)
        {
            try
            {
                await Clients.Group(e.GroupName).StatusChanged(e);
            }
            catch (Exception ex)
            {
                _log.LogError(ex.Message, e);
            }
        }

        #endregion

        private IDeviceHubClient GetWorkerClient(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new HubException("Не указана группа контроллера");

            if (!_groupWorkers.TryGetValue(groupName, out var workerConnId))
                throw new HubException($"Нет активного воркера для группы {groupName}.");

            return Clients.Client(workerConnId);
        }

        private Task BroadcastWorkerStateChangedAsync(string groupName, bool isOnline, string? reason = null)
        {
            if (!_workerStateStore.TryUpdate(groupName, isOnline, reason, out var notification))
                return Task.CompletedTask;

            return Clients.Group(groupName).WorkerStateChanged(notification);
        }

        private async Task ExecuteCommandAsync(string groupName, Func<Guid, Task> sendAsync)
        {
            var commandId = Guid.NewGuid();
            var tcs = new TaskCompletionSource<CommandCompletion>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pendingCommands[commandId] = tcs;

            try
            {
                await sendAsync(commandId);
                await WaitForCompletionAsync(commandId, groupName, Context.ConnectionAborted);
            }
            finally
            {
                _pendingCommands.TryRemove(commandId, out _);
            }
        }

        private async Task WaitForCompletionAsync(Guid commandId, string groupName, CancellationToken token)
        {
            if (!_pendingCommands.TryGetValue(commandId, out var tcs))
                return;

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            timeoutCts.CancelAfter(CommandTimeout);

            try
            {
                var completion = await tcs.Task.WaitAsync(timeoutCts.Token);
                if (!completion.IsSuccess)
                {
                    var message = string.IsNullOrWhiteSpace(completion.ErrorMessage)
                        ? $"Команда {commandId} для группы {groupName} завершилась с ошибкой."
                        : completion.ErrorMessage;
                    throw new HubException(message);
                }
            }
            catch (OperationCanceledException)
            {
                throw new HubException($"Таймаут ожидания команды {commandId} для группы {groupName}.");
            }
        }

    }
}

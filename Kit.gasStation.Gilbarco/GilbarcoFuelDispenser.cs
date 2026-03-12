//using KIT.GasStation.FuelDispenser.Hubs;
//using KIT.GasStation.FuelDispenser.Models;
//using KIT.GasStation.FuelDispenser.Services;
//using KIT.GasStation.Gilbarco.Utilities;
//using KIT.GasStation.HardwareConfigurations.Models;
//using KIT.GasStation.HardwareConfigurations.Services;
//using Microsoft.AspNetCore.SignalR.Client;
//using Serilog;
//using System.Collections.ObjectModel;
//using System.Data;
//using System.Diagnostics;
//using System.Text.RegularExpressions;

//namespace KIT.GasStation.Gilbarco
//{
//    /// <summary>
//    /// Сервис для работы с колонкой Gilbarco через двухпроводной протокол (TWOTP).
//    /// Поддерживает статус-опрос, авторизацию, пресеты, запрос транзакции и т.д.
//    /// </summary>
//    public sealed class GilbarcoFuelDispenser : FuelDispenserServiceBase
//    {
//        #region Private Members

//        private readonly IHubClient _hubClient;
//        private readonly AsyncManualResetEvent _pauseGate = new(initialState: true);
//        private readonly SemaphoreSlim _exclusive = new(1, 1);
//        private ISharedSerialPortService _sharedSerialPortService;
//        private HubConnection _hub;
//        private volatile bool _pollingEnabled;
//        private Task _pollingTask;
//        private PortLease? _lease;
//        private readonly object _pollLock = new();
//        private const int _defaultTimeoutMs = 3000;
//        private ILogger _logger;
//        private bool _hubHandlersRegistered;
//        private volatile bool _hardwareAvailable = true;
//        private string? _lastAvailabilityReason;
//        private int _hubRestartLoop;
//        private CancellationToken _token;

//        #endregion

//        #region Public Properties

//        public ObservableCollection<Controller> Controllers { get; private set; } = new();

//        #endregion

//        #region Constructors

//        //public GilbarcoFuelDispenser(Controller controller,
//        //    ISharedSerialPortService sharedSerialPortService,
//        //    IHubClient hubClient)
//        //    : base(controller, sharedSerialPortService, hubClient)
//        //{
//        //    _hubClient = hubClient;
//        //    _sharedSerialPortService = sharedSerialPortService;

//        //    CreateLogger();
//        //}

//        #endregion

//        #region Protected Overrides

//        protected override async Task OnOpenAsync(CancellationToken token)
//        {
//            try
//            {
//                _token = token;

//                _hub = _hubClient.Connection;
//                RegisterHubConnectionHandlers();

//                _logger.Information("Начало инициализации ТРК {Id}. Состояние HubConnection: {State}", Controller.Id, _hub?.State.ToString() ?? "null");

//                _logger.Debug("EnsureStartedAsync вызван. Текущее состояние: {State}", _hub.State);

//                _hub.On<StartPollingCommand>("StartPolling", async command =>
//                {
//                    await ExecuteHubCommandAsync(command.CommandId, command.GroupName,
//                        () => StartPollingAsync());
//                });

//                _hub.On<StopPollingCommand>("StopPolling", async command =>
//                {
//                    await ExecuteHubCommandAsync(command.CommandId, command.GroupName, StopPollingAsync);
//                });

//                _hub.On<Guid, Dictionary<string, decimal>>("SetPricesAsync", async (commandId, prices) =>
//                {
//                    var groupName = prices.Keys.FirstOrDefault() ?? string.Empty;
//                    await ExecuteHubCommandAsync(commandId, groupName, () => SetPricesAsync(prices));
//                });

//                _hub.On<Guid, string, decimal>("SetPriceAsync", async (commandId, groupName, price) =>
//                {
//                    await ExecuteHubCommandAsync(commandId, groupName,
//                        () => SetPriceAsync(groupName, price));
//                });

//                _hub.On<Guid, string>("InitializeConfigurationAsync", async (commandId, groupName) =>
//                {
//                    await ExecuteHubCommandAsync(commandId, groupName,
//                        () => InitializeConfigurationAsync(groupName));
//                });

//                _hub.On<string, decimal, bool>("StartFuelingAsync", async (groupName, sum, bySum) =>
//                {
//                    await StartRefuelingAsync(groupName, sum, bySum);
//                });

//                _hub.On<string>("CompleteFuelingAsync", async (groupName) =>
//                {
//                    await CompleteRefuelingAsync(groupName);
//                });

//                _hub.On<string>("StopFuelingAsync", async (groupName) =>
//                {
//                    await StopFuelingAsync(groupName);
//                });

//                _hub.On<string, decimal>("ResumeFuelingAsync", async (groupName, sum) =>
//                {
//                    await ResumeFuelingAsync(groupName, sum);
//                });

//                _hub.On<Guid, string>("GetCountersAsync", async (commandId, groupName) =>
//                {
//                    await ExecuteHubCommandAsync(commandId, groupName, () => GetCountersAsync());
//                });

//                await _hubClient.EnsureStartedAsync(token);

//                await JoinWorkerGroupsAsync();
//                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
//            }
//            catch (Exception e)
//            {
//                _logger.Error(e, "Ошибка при открытии подключения к Gilbarco");
//            }
//        }

//        protected override async Task OnTickAsync()
//        {
//            _logger.Information("Gilbarco TWOTP polling запущен на порту {Port}", Controller.ComPort);

//            await _pauseGate.WaitAsync(_token);

//            foreach (var address in Controller.Columns
//                                   .Select(c => c.Address)
//                                   .Distinct()
//                                   .OrderBy(a => a))
//            {
//                Controllers.Add(new Controller
//                {
//                    Settings = (GilbarcoControllerSettings)Controller.Settings,
//                    Address = address,
//                    Columns = new(Controller.Columns.Where(c => c.Address == address)),
//                });
//            }

//            if (!Controllers.Any()) return;

//            while (!_token.IsCancellationRequested && _pollingEnabled)
//            {
//                try
//                {
//                    foreach (var controller in Controllers)
//                    {
//                        _pollingResumedEvent.Wait(_token);

//                        var status = await PollingStatusAsync(controller);

//                        controller.Settings.SetStatus(status);

//                        switch (status)
//                        {
//                            case GilbarcoStatus.DataError:
//                                break;
//                            case GilbarcoStatus.Off:
//                                await PublishStatusAsync(controller, status);
//                                break;
//                            case GilbarcoStatus.Call:
//                                await PublishStatusAsync(controller, status);
//                                await PollingExtendedStatusAsync(controller);
//                                break;
//                            case GilbarcoStatus.AuthorizedNotDelivering:
//                                await PublishAuthorizedStatusAsync(controller, status);
//                                break;
//                            case GilbarcoStatus.Busy:
//                                await PublishBussyStatusAsync(controller);
//                                break;
//                            case GilbarcoStatus.TransactionCompletePeot:
//                                await PublishTransactionCompletePeot(controller);
//                                break;
//                            case GilbarcoStatus.TransactionCompleteFeot:
//                                await PublishTransactionCompletePeot(controller);
//                                break;
//                            case GilbarcoStatus.PumpStop:
//                                await PublishPumpStop(controller, status);
//                                break;
//                            case GilbarcoStatus.SendData:
//                                break;
//                            default:
//                                break;
//                        }
//                    }
//                }
//                catch (OperationCanceledException e)
//                {
//                    _logger.Error(e, "Операция опроса TWOTP была отменена");
//                    break;
//                }
//                catch (Exception e)
//                {
//                    _logger.Error(e, "Ошибка в цикле опроса TWOTP");
//                }

//                if (!_pollingEnabled) break;
//            }
//        }

//        protected override Task OnCloseAsync()
//        {
//            return Task.CompletedTask;
//        }

//        public override async ValueTask DisposeAsync()
//        {
//            (_logger as IDisposable)?.Dispose();
//            await base.DisposeAsync();
//        }

//        #endregion

//        #region Пауза и продолжения опроса

//        private readonly ManualResetEventSlim _pollingResumedEvent = new(true);
//        private readonly SemaphoreSlim _commandGate = new(1, 1);
//        private readonly object _liftedStateLock = new();
//        private readonly Dictionary<int, bool> _liftedStates = new();

//        private async Task PausePollingAsync()
//        {
//            _pollingResumedEvent.Reset();

//            // ⛔ ждём, пока текущая команда ДОРАБОТАЕТ
//            await _commandGate.WaitAsync();
//            _commandGate.Release();
//        }

//        private async Task ResumePollingAsync()
//        {
//            _pollingResumedEvent.Set();
//            await Task.CompletedTask;
//        }

//        private async Task ExecuteCommandSafeAsync(
//            Func<Task> command)
//        {
//            await _commandGate.WaitAsync(_token);
//            try
//            {
//                await command(); // <-- вот тут команда гарантированно ДОРАБАТЫВАЕТ
//            }
//            finally
//            {
//                _commandGate.Release();
//            }
//        }

//        #endregion

//        #region Business Logic

//        private async Task InitializeConfigurationAsync(string groupName)
//        {
//            try
//            {
//                await PausePollingAsync();

//                var controller = Controllers.FirstOrDefault(c => c.Columns.Any(col => col.GroupName == groupName));

//                if (controller == null) return;

//                var dataNext = ProtocolParser.BuildPackCommand(Command.DataNext, controller.Address);

//                var rx = await _sharedSerialPortService.WriteReadAsync(
//                        dataNext,
//                        expectedRxLength: 2, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3,
//                        ct: _token);

//                var status = ProtocolParser.GetStatus(rx);

//                if (status is GilbarcoStatus.SendData)
//                {
//                    var sendData = ProtocolParser.BuildMiscPumpDataBlock();

//                    _logger.Information("[Tx] {Frame}", BitConverter.ToString(dataNext.Concat(sendData).ToArray()));

//                    rx = await _sharedSerialPortService.WriteReadAsync(
//                        sendData,
//                        expectedRxLength: sendData.Length + 27, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3,
//                        ct: _token);

//                    rx = ProtocolParser.RemoveEcho(rx, sendData);

//                    _logger.Information("[Rx] {Response}", BitConverter.ToString(rx));

//                    var config = ProtocolParser.ParseConfig(rx);

//                    controller.Settings.SetConfig(config);
//                }
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task SetPricesAsync(Dictionary<string, decimal> prices)
//        {
//            try
//            {
//                await PausePollingAsync();

//                foreach (var (group, price) in prices)
//                {
//                    var column = Controller.Columns.FirstOrDefault(c => c.GroupName == group);
//                    if (column == null) return;

//                    var dataNext = ProtocolParser.BuildPackCommand(Command.DataNext, column.Address);

//                    var rx = await _sharedSerialPortService.WriteReadAsync(
//                            dataNext,
//                            expectedRxLength: 2, // минимум 1 слово (1 байт)
//                            writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                            readTimeoutMs: _defaultTimeoutMs,
//                            maxRetries: 3);

//                    var status = ProtocolParser.GetStatus(rx);

//                    if (status is GilbarcoStatus.SendData)
//                    {
//                        var sendData = ProtocolParser.BuildPriceChangeBlock(column, price);

//                        _logger.Information("[Tx] {Frame}", BitConverter.ToString(dataNext.Concat(sendData).ToArray()));

//                        rx = await _sharedSerialPortService.WriteReadAsync(
//                            sendData,
//                            expectedRxLength: 0, // минимум 1 слово (1 байт)
//                            writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                            readTimeoutMs: _defaultTimeoutMs,
//                            maxRetries: 3);
//                    }
//                }
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task SetPriceAsync(string groupName, decimal price)
//        {
//            try
//            {
//                await PausePollingAsync();

//                var column = Controller.Columns.FirstOrDefault(c => c.GroupName == groupName);
//                if (column == null) return;

//                var dataNext = ProtocolParser.BuildPackCommand(Command.DataNext, column.Address);

//                var rx = await _sharedSerialPortService.WriteReadAsync(
//                        dataNext,
//                        expectedRxLength: 2, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3);

//                var status = ProtocolParser.GetStatus(rx);

//                if (status is GilbarcoStatus.SendData)
//                {
//                    var sendData = ProtocolParser.BuildPriceChangeBlock(column, price);

//                    _logger.Information("[Tx] {Frame}", BitConverter.ToString(dataNext.Concat(sendData).ToArray()));

//                    rx = await _sharedSerialPortService.WriteReadAsync(
//                        sendData,
//                        expectedRxLength: 0, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3);
//                }
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task StartRefuelingAsync(string groupName, decimal amount, bool bySum)
//        {
//            try
//            {
//                await PausePollingAsync();

//                await Task.Delay(200);

//                var controller = Controllers.FirstOrDefault(c => c.Columns.Any(col => col.GroupName == groupName));

//                if (controller == null) return;

//                var dataNext = ProtocolParser.BuildPackCommand(Command.DataNext, controller.Address);

//                var rx = await _sharedSerialPortService.WriteReadAsync(
//                        dataNext,
//                        expectedRxLength: 2, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3,
//                        ct: _token);

//                var status = ProtocolParser.GetStatus(rx);

//                if (status is GilbarcoStatus.SendData)
//                {
//                    var column = controller.Columns.First(c => c.GroupName == groupName);
//                    var grade = column.Nozzle;
//                    var presetBlock = ProtocolParser.BuildPresetBlock(controller, column, grade, amount, bySum);

//                    _logger.Information("[Tx] {Frame} (старт налива)", BitConverter.ToString(dataNext.Concat(presetBlock).ToArray()));

//                    rx = await _sharedSerialPortService.WriteReadAsync(
//                        presetBlock,
//                        expectedRxLength: 0, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3,
//                        ct: _token);

//                    status = await PollingStatusAsync(controller);

//                    if (status is GilbarcoStatus.Off or GilbarcoStatus.Call)
//                    {
//                        var authorize = ProtocolParser.BuildPackCommand(Command.Authorization, controller.Address);

//                        _logger.Information("[Tx] {Frame}", BitConverter.ToString(authorize));

//                        rx = await _sharedSerialPortService.WriteReadAsync(
//                            authorize,
//                            expectedRxLength: 0, // минимум 1 слово (1 байт)
//                            writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                            readTimeoutMs: _defaultTimeoutMs,
//                            maxRetries: 3,
//                            ct: _token);

//                        column.Settings.Status = ColumnStatus.Waiting;
//                    }
//                }
//                else
//                {
//                    _logger.Warning("Не удалось начать заправку для группы {GroupName}, статус ТРК: {Status}", groupName, status);
//                    _ = new InvalidOperationException($"Невозможно начать заправку, статус ТРК: {status}");
//                }
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task CompleteRefuelingAsync(string groupName)
//        {
//            try
//            {
//                await PausePollingAsync();

//                await Task.Delay(200);

//                var controller = Controllers.FirstOrDefault(c => c.Columns.Any(col => col.GroupName == groupName));

//                if (controller == null) return;

//                var dataNext = ProtocolParser.BuildPackCommand(Command.PumpStop, controller.Address);

//                _logger.Information("[Tx] {Frame}", BitConverter.ToString(dataNext));

//                var rx = await _sharedSerialPortService.WriteReadAsync(
//                        dataNext,
//                        expectedRxLength: 1, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3,
//                        ct: _token);

//                await _hub.InvokeAsync("OnWaitingAsync", groupName, cancellationToken: _token);
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task StopFuelingAsync(string groupName)
//        {
//            try
//            {
//                await PausePollingAsync();

//                var controller = Controllers.FirstOrDefault(c => c.Columns.Any(col => col.GroupName == groupName));

//                if (controller == null) return;

//                var command = ProtocolParser.BuildPackCommand(Command.PumpStop, controller.Address);

//                _logger.Information("[Tx] {Frame}", BitConverter.ToString(command));

//                var rx = await _sharedSerialPortService.WriteReadAsync(
//                        command,
//                        expectedRxLength: 1, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3,
//                        ct: _token);

//                await _hub.InvokeAsync("OnWaitingAsync", groupName, cancellationToken: _token);
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task ResumeFuelingAsync(string groupName, decimal sum)
//        {
//            try
//            {
//                await PausePollingAsync();

//                await Task.Delay(200);

//                var controller = Controllers.FirstOrDefault(c => c.Columns.Any(col => col.GroupName == groupName));

//                if (controller == null) return;

//                var column = Controller.Columns.First(c => c.GroupName == groupName);

//                if (column.Settings.Status is ColumnStatus.PumpStop)
//                {
//                    var resume = ProtocolParser.BuildPackCommand(Command.Authorization, controller.Address);

//                    _logger.Information("[Tx] {Frame}", BitConverter.ToString(resume));

//                    var rxResume = await _sharedSerialPortService.WriteReadAsync(
//                        resume,
//                        expectedRxLength: 0, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3,
//                        ct: _token);

//                    column.Settings.Status = ColumnStatus.Waiting;

//                    return;
//                }

//                var dataNext = ProtocolParser.BuildPackCommand(Command.DataNext, controller.Address);

//                var rx = await _sharedSerialPortService.WriteReadAsync(
//                        dataNext,
//                        expectedRxLength: 2, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3,
//                        ct: _token);

//                var status = ProtocolParser.GetStatus(rx);

//                if (status is GilbarcoStatus.SendData)
//                {
//                    var grade = column.Nozzle;
//                    var presetBlock = ProtocolParser.BuildPresetBlock(controller, column, grade, sum, true);

//                    _logger.Information("[Tx] {Frame} (старт налива)", BitConverter.ToString(dataNext.Concat(presetBlock).ToArray()));

//                    rx = await _sharedSerialPortService.WriteReadAsync(
//                        presetBlock,
//                        expectedRxLength: 0, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3,
//                        ct: _token);

//                    status = await PollingStatusAsync(controller);

//                    if (status is GilbarcoStatus.Off or GilbarcoStatus.Call)
//                    {
//                        var authorize = ProtocolParser.BuildPackCommand(Command.Authorization, controller.Address);

//                        _logger.Information("[Tx] {Frame}", BitConverter.ToString(authorize));

//                        rx = await _sharedSerialPortService.WriteReadAsync(
//                            authorize,
//                            expectedRxLength: 0, // минимум 1 слово (1 байт)
//                            writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                            readTimeoutMs: _defaultTimeoutMs,
//                            maxRetries: 3,
//                            ct: _token);

//                        column.Settings.Status = ColumnStatus.Waiting;
//                    }
//                }
//                else
//                {
//                    _logger.Warning("Не удалось начать заправку для группы {GroupName}, статус ТРК: {Status}", groupName, status);
//                    _ = new InvalidOperationException($"Невозможно начать заправку, статус ТРК: {status}");
//                }

//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        /// <summary>
//        /// Опрос статуса ТРК
//        /// </summary>
//        private async Task<GilbarcoStatus> PollingStatusAsync(Controller controller)
//        {
//            var frame = ProtocolParser.BuildPackCommand(Command.Status, controller.Address);
//            _logger.Information("[Tx] {Frame}", BitConverter.ToString(frame));

//            var rx = await _sharedSerialPortService.WriteReadAsync(
//                        frame,
//                        expectedRxLength: 2, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3,
//                        ct: _token);

//            rx = ProtocolParser.RemoveEcho(rx, frame);

//            _logger.Information("[Rx] {Response}", BitConverter.ToString(rx));

//            return ProtocolParser.GetStatus(rx);
//        }

//        /// <summary>
//        /// Публикация статуса ТРК на Hub
//        /// </summary>
//        private async Task PublishStatusAsync(Controller controller, GilbarcoStatus status)
//        {
//            var column = controller.Columns.First();

//            if (status is GilbarcoStatus.Off)
//            {
//                var liftedColumn = controller.Columns.FirstOrDefault(c => c.IsLifted);

//                if (liftedColumn != null)
//                {
//                    liftedColumn.IsLifted = false;
//                    await _hub.InvokeAsync("ColumnLiftedChanged", liftedColumn.GroupName, liftedColumn.IsLifted, _token);
//                    controller.Settings.SetIsLifted(false);
//                }
//            }

//            var parsed = new StatusResponse
//            {
//                Status = ProtocolParser.GilbarcoStatusToNozzleStatusConverter(status),
//                GroupName = column.GroupName,
//            };

//            await _hub.InvokeAsync("PublishStatus", parsed, cancellationToken: _token);
//        }

//        private async Task PublishBussyStatusAsync(Controller controller)
//        {
//            var column = controller.Columns.FirstOrDefault(c => c.Settings.Status == ColumnStatus.Waiting);

//            if (column == null)
//            {
//                _logger.Information("Пистолет не найдено 687");
//                return;
//            }

//            var command = ProtocolParser.BuildPackCommand(Command.RealTimeMoneyRequest, controller.Address);

//            _logger.Information("[Tx] {Frame}", BitConverter.ToString(command));

//            var rx = await _sharedSerialPortService.WriteReadAsync(
//                    command,
//                    expectedRxLength: command.Length + 6, // минимум 1 слово (1 байт)
//                    writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                    readTimeoutMs: _defaultTimeoutMs,
//                    maxRetries: 3,
//                    ct: _token);

//            rx = ProtocolParser.RemoveEcho(rx, command);

//            _logger.Information("[Rx] {Response}", BitConverter.ToString(rx));

//            decimal money = ProtocolParser.ParseRealTimeMoney(rx);

//            var fuelingResponse = new FuelingResponse
//            {
//                GroupName = column.GroupName,
//                Quantity = money,
//            };

//            await _hub.InvokeAsync("OnFuelingAsync", fuelingResponse, cancellationToken: _token);
//        }

//        private async Task PublishPumpStop(Controller controller, GilbarcoStatus status)
//        {
//            var column = controller.Columns.FirstOrDefault(c => c.Settings.Status == ColumnStatus.Waiting);

//            if (column == null) return;

//            column.Settings.Status = ColumnStatus.PumpStop;

//            var statusResponse = new StatusResponse
//            {
//                Status = ProtocolParser.GilbarcoStatusToNozzleStatusConverter(status),
//                GroupName = column.GroupName,
//            };

//            await _hub.InvokeAsync("PublishStatus", statusResponse, cancellationToken: _token);
//        }

//        private async Task PublishTransactionCompletePeot(Controller controller)
//        {
//            try
//            {
//                await PausePollingAsync();

//                var column = controller.Columns.FirstOrDefault(c => c.Settings.Status == ColumnStatus.Waiting);

//                if (column == null) return;

//                column.Settings.Status = ColumnStatus.Unknown;

//                await _hub.InvokeAsync("OnWaitingAsync", column.GroupName, cancellationToken: _token);

//                _logger.Information("Вызван метод Ожидания");

//                var command = ProtocolParser.BuildPackCommand(Command.TransactionDataRequest, controller.Address);

//                _logger.Information("[Tx] {Frame}", BitConverter.ToString(command));

//                var rx = await _sharedSerialPortService.WriteReadAsync(
//                        command,
//                        expectedRxLength: command.Length + 33, // минимум 1 слово (1 байт)
//                        writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                        readTimeoutMs: _defaultTimeoutMs,
//                        maxRetries: 3,
//                        ct: _token);

//                rx = ProtocolParser.RemoveEcho(rx, command);

//                _logger.Information("[Rx] {Response}", BitConverter.ToString(rx));

//                var requestTransactionData = ProtocolParser.ParseRequestTransactionData(rx);

//                await _hub.InvokeAsync("OnCompletedFuelingAsync", column.GroupName, requestTransactionData.Volume, cancellationToken: _token);

//                _logger.Information("Volume {Volume}, Price {Price}, Amount {Amount}", requestTransactionData.Volume, requestTransactionData.Price, requestTransactionData.Amount);
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        private async Task PublishAuthorizedStatusAsync(Controller controller, GilbarcoStatus status)
//        {
//            var column = controller.Columns.FirstOrDefault(c => c.Settings.Status == ColumnStatus.Waiting);

//            if (column == null) return;

//            var parsed = new StatusResponse
//            {
//                Status = ProtocolParser.GilbarcoStatusToNozzleStatusConverter(status),
//                GroupName = column.GroupName,
//            };

//            await _hub.InvokeAsync("PublishStatus", parsed, cancellationToken: _token);
//        }

//        /// <summary>
//        /// Расширенный опрос статуса (подъём/опускание пистолета)
//        /// </summary>
//        private async Task PollingExtendedStatusAsync(Controller controller)
//        {
//            var isLifted = controller.Settings.GetIsLifted();

//            if (!isLifted)
//            {
//                controller.Settings.SetIsLifted(true);
//                var dataNext = ProtocolParser.BuildPackCommand(Command.DataNext, controller.Address);

//                var rx = await _sharedSerialPortService.WriteReadAsync(
//                            dataNext,
//                            expectedRxLength: 2, // минимум 1 слово (1 байт)
//                            writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                            readTimeoutMs: _defaultTimeoutMs,
//                            maxRetries: 3,
//                            ct: _token);

//                var status = ProtocolParser.GetStatus(rx);

//                if (status is GilbarcoStatus.SendData && !controller.Columns.Any(c => c.IsLifted))
//                {
//                    var sendData = ProtocolParser.BuildExtendedStatusBlock();

//                    _logger.Information("[Tx] {Frame}", BitConverter.ToString(dataNext.Concat(sendData).ToArray()));

//                    rx = await _sharedSerialPortService.WriteReadAsync(
//                            sendData,
//                            expectedRxLength: sendData.Length + 19, // минимум 1 слово (1 байт)
//                            writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                            readTimeoutMs: _defaultTimeoutMs,
//                            maxRetries: 3,
//                            ct: _token);

//                    rx = ProtocolParser.RemoveEcho(rx, sendData);

//                    _logger.Information("[Rx] {Response}", BitConverter.ToString(rx));

//                    var extendedStatus = ProtocolParser.ParseExtendedStatus(rx);

//                    var column = controller.Columns.First(c => c.Nozzle == extendedStatus.SelectedGrade);
//                    column.IsLifted = extendedStatus.IsNozzleLifted;

//                    await _hub.InvokeAsync("ColumnLiftedChanged", column.GroupName, column.IsLifted, _token);
//                }
//            }
//        }

//        /// <summary>
//        /// Получение счётчиков по одному пистолету
//        /// </summary>
//        private async Task GetCountersAsync()
//        {
//            try
//            {
//                await PausePollingAsync();

//                await Task.Delay(200);

//                foreach (var controller in Controllers.OrderBy(c => c.Address))
//                {
//                    if (controller != null && controller.Settings is GilbarcoControllerSettings settings)
//                    {
//                        var firstColumn = controller.Columns.First();

//                        var countCommand = ProtocolParser.BuildPackCommand(Command.PumpTotalsDataRequest, controller.Address);

//                        // расчетный размер ответ, каждый пистолет добавляет 30 байт
//                        var totalBytes = 34 + (controller.Columns.Count - 1) * 30; // расчетный размер ответа

//                        _logger.Information("[Tx] {Frame}", BitConverter.ToString(countCommand));

//                        var rx = await _sharedSerialPortService.WriteReadTwotpFrameAsync(
//                                    countCommand,
//                                    writeToReadDelayMs: 68, // TWOTP: min delay между словами — 68 мс
//                                    readTimeoutMs: _defaultTimeoutMs,
//                                    maxRetries: 3,
//                                    ct: _token);

//                        rx = ProtocolParser.RemoveEcho(rx, countCommand);

//                        _logger.Information("[Rx] {Response}", BitConverter.ToString(rx));

//                        var counters = ProtocolParser.ParseCounters(rx);

//                        var counterData = new List<CounterData>();

//                        foreach (var item in counters)
//                        {
//                            var column = controller.Columns.FirstOrDefault(c => c.Nozzle == item.Nozzle);
//                            if (column == null)
//                            {
//                                return;
//                            }

//                            counterData.Add(new CounterData
//                            {
//                                Counter = item.Counter,
//                                GroupName = column.GroupName
//                            });
//                        }

//                        await _hub.InvokeAsync("OnCountersUpdated", firstColumn.GroupName, counterData);
//                    }
//                }
//            }
//            finally
//            {
//                await ResumePollingAsync();
//            }
//        }

//        #endregion

//        #region Polling Control

//        private async Task StartPollingAsync()
//        {
//            lock (_pollLock)
//            {
//                if (_pollingEnabled) return;
//            }

//            try
//            {
//                lock (_pollLock)
//                {
//                    _pollingEnabled = true;
//                }

//                _pollingTask = OnTickAsync();
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Не удалось запустить TWOTP polling");
//                lock (_pollLock) _pollingEnabled = false;
//                if (_lease != null)
//                {
//                    await _lease.DisposeAsync();
//                    _lease = null;
//                }
//            }
//        }

//        private async Task StopPollingAsync()
//        {
//            Task? toAwait = null;
//            lock (_pollLock)
//            {
//                if (!_pollingEnabled) return;
//                _pollingEnabled = false;
//                toAwait = _pollingTask;
//            }

//            if (toAwait != null)
//                await toAwait;

//            if (_lease != null)
//            {
//                await _lease.DisposeAsync();
//                _lease = null;
//            }
//            _sharedSerialPortService = null!;
//        }

//        #endregion

//        #region Hub

//        private void RegisterHubConnectionHandlers()
//        {
//            if (_hubHandlersRegistered || _hub is null)
//                return;

//            _hub.Reconnecting += OnHubReconnecting;
//            _hub.Reconnected += OnHubReconnected;
//            _hub.Closed += OnHubClosed;
//            _hubHandlersRegistered = true;
//        }

//        private Task OnHubReconnecting(Exception? error)
//        {
//            _logger.Warning("Потеряно соединение с сервером: {Message}", error?.Message ?? "unknown");
//            return Task.CompletedTask;
//        }

//        private async Task OnHubReconnected(string? connectionId)
//        {
//            _logger.Information("Переподключен с сервером. ConnectionId={ConnectionId}", connectionId);
//            try
//            {
//                await JoinWorkerGroupsAsync();
//                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Не удалось повторно присоединиться к группам после переподключения");
//            }
//        }

//        private Task OnHubClosed(Exception? error)
//        {
//            _logger.Error(error, "Соединение с SignalR было закрыто");
//            return RestartHubConnectionLoopAsync();
//        }

//        private Task RestartHubConnectionLoopAsync()
//        {
//            if (_hub is null)
//                return Task.CompletedTask;

//            if (Interlocked.CompareExchange(ref _hubRestartLoop, 1, 0) != 0)
//                return Task.CompletedTask;

//            return Task.Run(async () =>
//            {
//                try
//                {
//                    while (_hub.State != HubConnectionState.Connected)
//                    {
//                        try
//                        {
//                            await _hub.StartAsync();
//                            await JoinWorkerGroupsAsync();
//                            await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
//                            break;
//                        }
//                        catch (Exception ex)
//                        {
//                            _logger.Error(ex, "Не удалось переподключиться к SignalR, повтор через 5 секунд");
//                            await Task.Delay(TimeSpan.FromSeconds(5));
//                        }
//                    }
//                }
//                finally
//                {
//                    Interlocked.Exchange(ref _hubRestartLoop, 0);
//                }
//            });
//        }

//        private async Task JoinWorkerGroupsAsync()
//        {
//            if (_hub is null || Controller?.Columns is null)
//                return;

//            // Ждем, пока соединение станет активным
//            var timeout = TimeSpan.FromSeconds(10);
//            var stopwatch = Stopwatch.StartNew();

//            while (_hub.State != HubConnectionState.Connected && stopwatch.Elapsed < timeout)
//            {
//                await Task.Delay(100);
//            }

//            if (_hub.State != HubConnectionState.Connected)
//                throw new InvalidOperationException("Не удалось подключиться к SignalR за отведенное время");

//            foreach (var item in Controller.Columns)
//            {
//                if (string.IsNullOrWhiteSpace(item.GroupName))
//                {
//                    item.GroupName = $"{Controller.Name}/{item.Name}";
//                }

//                await _hub.InvokeAsync("JoinController", item.GroupName, true);
//            }
//        }

//        private async Task BroadcastWorkerAvailabilityAsync(bool isAvailable, string? reason = null, bool force = false)
//        {
//            var sanitizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

//            if (!force &&
//                _hardwareAvailable == isAvailable &&
//                string.Equals(_lastAvailabilityReason ?? string.Empty, sanitizedReason ?? string.Empty, StringComparison.Ordinal))
//            {
//                return;
//            }

//            _hardwareAvailable = isAvailable;
//            _lastAvailabilityReason = sanitizedReason;

//            if (_hub is null || _hub.State != HubConnectionState.Connected)
//                return;

//            if (Controller?.Columns is null)
//                return;

//            var groups = Controller.Columns
//                .Where(c => !string.IsNullOrWhiteSpace(c.GroupName))
//                .Select(c => c.GroupName!);

//            var tasks = groups.Select(group => SendAvailabilityAsync(group, isAvailable, sanitizedReason));
//            await Task.WhenAll(tasks);
//        }

//        private async Task SendAvailabilityAsync(string groupName, bool isAvailable, string? reason)
//        {
//            try
//            {
//                var report = new WorkerAvailabilityReport
//                {
//                    GroupName = groupName,
//                    IsAvailable = isAvailable,
//                    Reason = reason
//                };
//                await _hub.InvokeAsync("ReportWorkerAvailability", report);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Не удалось отправить состояние worker для {Group}", groupName);
//            }
//        }

//        private async Task ExecuteHubCommandAsync(Guid commandId, string groupName, Func<Task> action)
//        {
//            Exception? error = null;
//            try
//            {
//                await action();
//            }
//            catch (Exception ex)
//            {
//                error = ex;
//                _logger.Error(ex, "Ошибка выполнения команды {CommandId} для {Group}", commandId, groupName);
//            }

//            await ReportCommandCompletedAsync(commandId, groupName, error);
//        }

//        private async Task ReportCommandCompletedAsync(Guid commandId, string groupName, Exception? error)
//        {
//            if (_hub is null || _hub.State != HubConnectionState.Connected)
//                return;

//            var completion = new CommandCompletion
//            {
//                CommandId = commandId,
//                GroupName = groupName,
//                IsSuccess = error is null,
//                ErrorMessage = error?.Message
//            };

//            try
//            {
//                await _hub.InvokeAsync("ReportCommandCompleted", completion);
//            }
//            catch (Exception ex)
//            {
//                _logger.Error(ex, "Не удалось отправить подтверждение команды {CommandId}", commandId);
//            }
//        }

//        private async Task PublishStatus()
//        {

//        }

//        #endregion

//        #region Logging

//        private void CreateLogger()
//        {
//            var logRoot = Path.Combine(AppContext.BaseDirectory, "logs", "trk");
//            Directory.CreateDirectory(logRoot);

//            string safeController = Sanitize(Controller.Name);
//            string safePort = Sanitize(Controller.ComPort);
//            string controllerId = Controller.Id == Guid.Empty ? "noid" : Controller.Id.ToString("N");
//            string fileName = $"TRK_Gilbarco_{safeController}_{safePort}_{controllerId}_{Address}.log";
//            string path = Path.Combine(logRoot, fileName);

//            _logger = new LoggerConfiguration()
//                .MinimumLevel.Debug()
//                .Enrich.WithProperty("Controller", Controller.Name)
//                .Enrich.WithProperty("Address", Address)
//                .Enrich.WithProperty("ComPort", Controller.ComPort)
//                .WriteTo.File(
//                    path: path,
//                    rollingInterval: RollingInterval.Day,
//                    retainedFileCountLimit: 14,
//                    shared: true,
//                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
//                .CreateLogger();

//            _logger.Information("Инициализация Gilbarco TWOTP для {Controller}/{Address}", Controller.Name, Address);
//        }

//        private static string Sanitize(string s)
//        {
//            if (string.IsNullOrWhiteSpace(s)) return "UNNAMED";
//            s = Regex.Replace(s, @"[^\w\-\.\(\) ]+", "_");
//            return s.Length > 80 ? s[..80] : s;
//        }

//        #endregion
//    }
//}

using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.Gilbarco.Helpers;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.Diagnostics;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace KIT.GasStation.Gilbarco
{
    /// <summary>
    /// Сервис для работы с колонкой Gilbarco через двухпроводной протокол (TWOTP).
    /// Поддерживает статус-опрос, авторизацию, пресеты, запрос транзакции и т.д.
    /// </summary>
    public sealed class GilbarcoFuelDispenser : FuelDispenserServiceBase
    {
        #region Private Members

        private readonly IHubClient _hubClient;
        private readonly AsyncManualResetEvent _pauseGate = new(initialState: true);
        private readonly SemaphoreSlim _exclusive = new(1, 1);
        private ISharedSerialPortService _sharedSerialPortService;
        private HubConnection _hub;
        private volatile bool _pollingEnabled;
        private Task _pollingTask;
        private PortLease? _lease;
        private readonly object _pollLock = new();
        private const int _defaultTimeoutMs = 3000;
        private ILogger _logger;
        private bool _hubHandlersRegistered;
        private volatile bool _hardwareAvailable = true;
        private string? _lastAvailabilityReason;
        private int _hubRestartLoop;

        #endregion

        #region Constructors

        public GilbarcoFuelDispenser(Controller controller,
            ISharedSerialPortService sharedSerialPortService,
            IHubClient hubClient)
            : base(controller, sharedSerialPortService, hubClient)
        {
            _hubClient = hubClient;
            _sharedSerialPortService = sharedSerialPortService;

            CreateLogger();
        }

        #endregion

        #region Protected Overrides

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            try
            {
                _logger.Information("Начало инициализации ТРК {Id}. Состояние HubConnection: {State}", Controller.Id, _hub?.State.ToString() ?? "null");

                _hub = _hubClient.Connection;
                RegisterHubConnectionHandlers();

                _logger.Debug("EnsureStartedAsync вызван. Текущее состояние: {State}", _hub.State);

                _hub.On<StartPollingCommand>("StartPolling", async _ => await StartPollingAsync(token));
                _hub.On<StopPollingCommand>("StopPolling", async _ => await StopPollingAsync());

                _hub.On<string, decimal>("SetPriceAsync", async (groupName, price) =>
                    await SetPriceAsync(groupName, price));

                _hub.On<string, decimal, bool>("StartFuelingAsync", async (groupName, sum, bySum) =>
                    await StartRefuelingAsync(groupName, sum, bySum));

                _hub.On<string>("CompleteFuelingAsync", async _ =>
                    await CompleteRefuelingAsync());

                _hub.On<string, bool>("ChangeControlModeAsync", async (groupName, isProgramMode) =>
                {
                    await ChangeControlModeAsync(groupName, isProgramMode);
                });

                _hub.On<string>("StopFuelingAsync", async (groupName) =>
                {
                    await StopFuelingAsync(groupName);
                });

                _hub.On<string>("ResumeFuelingAsync", async (groupName) =>
                {
                    await ResumeFuelingAsync(groupName);
                });

                _hub.On<string>("GetStatusByAddressAsync", async (groupName) =>
                {
                    await GetStatusByAddressAsync(groupName);
                });

                _hub.On<string>("GetCountersAsync", async (groupName) =>
                {
                    var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                    if (column is not null)
                    {
                        await ExecuteCommandAsync(Command.CounterLiter, Address, column.LanfengAddress);
                    }
                });

                _hub.On<string>("PausePollingAsync", async (groupName) =>
                {
                    if (Columns.Any(c => c.GroupName == groupName))
                    {
                        await PausePollingAsync();
                    }
                });

                _hub.On<string>("ResumePollingAsync", async (groupName) =>
                {
                    if (Columns.Any(c => c.GroupName == groupName))
                    {
                        await ResumePollingAsync();
                    }
                });

                await _hubClient.EnsureStartedAsync(token);

                await JoinWorkerGroupsAsync();
                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Ошибка при открытии подключения к Gilbarco");
            }
        }

        protected override async Task OnTickAsync(CancellationToken token)
        {
            _logger.Information("Gilbarco TWOTP polling запущен на порту {Port}", Controller.ComPort);

            // Инициализация: запрос версии через Special Function 001
            await _pauseGate.WaitAsync(token);

            var addresses = Controller.Columns
                .Select(c => c.Address)
                .Distinct()
                .OrderBy(a => a)
                .ToArray();

            if (addresses == null) return;

            while (!token.IsCancellationRequested && _pollingEnabled)
            {
                try
                {
                    foreach (int address in addresses)
                    {
                        _pollingResumedEvent.Wait(token);

                        await ExecuteCommandSafeAsync(() => 
                        ExecuteCommandAsync(Command.Status, address, 0x001, expectedLength: 2, ct: token), token); // Version Request
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Ошибка в цикле опроса TWOTP");
                }

                if (!_pollingEnabled) break;
            }
        }

        protected override Task OnCloseAsync()
        {
            return Task.CompletedTask;
        }

        public override async ValueTask DisposeAsync()
        {
            (_logger as IDisposable)?.Dispose();
            await base.DisposeAsync();
        }

        #endregion

        #region Пауза и продолжения опроса

        private readonly ManualResetEventSlim _pollingResumedEvent = new(true);
        private readonly SemaphoreSlim _commandGate = new(1, 1);

        private async Task PausePollingAsync()
        {
            _pollingResumedEvent.Reset();

            // ⛔ ждём, пока текущая команда ДОРАБОТАЕТ
            await _commandGate.WaitAsync();
            _commandGate.Release();
        }

        private async Task ResumePollingAsync()
        {
            _pollingResumedEvent.Set();
            await Task.CompletedTask;
        }

        private async Task ExecuteCommandSafeAsync(
            Func<Task> command,
            CancellationToken token)
        {
            await _commandGate.WaitAsync(token);
            try
            {
                await command(); // <-- вот тут команда гарантированно ДОРАБАТЫВАЕТ
            }
            finally
            {
                _commandGate.Release();
            }
        }

        #endregion

        #region Command Execution

        private async Task ExecuteCommandAsync(
            Command cmd,
            int pumpId,
            int subCommandOrData,
            decimal? value = null,
            int expectedLength = 0,
            bool bySum = true,
            CancellationToken ct = default)
        {
            if (_sharedSerialPortService is null)
                throw new InvalidOperationException("COM-порт не инициализирован");

            await _pauseGate.WaitAsync(ct);
            await _exclusive.WaitAsync(ct);

            try
            {
                var frame = ProtocolParser.BuildRequest(cmd, pumpId, subCommandOrData, value, bySum);
                _logger.Information("[Tx] {Frame}", BitConverter.ToString(frame));

                var rx = await _sharedSerialPortService.WriteReadAsync(
                    frame,
                    expectedRxLength: expectedLength > 0 ? expectedLength : 1, // минимум 1 слово (1 байт)
                    writeToReadDelayMs: 100, // TWOTP: min delay между словами — 68 мс
                    readTimeoutMs: _defaultTimeoutMs,
                    maxRetries: 3,
                    ct: ct);

                _logger.Information("[Rx] {Response}", BitConverter.ToString(rx));

                var parsed = ProtocolParser.ParseResponse(rx);
                if (parsed != null)
                {
                    parsed.Group = Controller.Columns.FirstOrDefault()?.GroupName;
                    await _hub.InvokeAsync("PublishStatus", parsed, parsed.Group);

                    // Обновление статуса
                    Status = parsed.Status;
                }
            }
            catch (Exception ex)
            {

            }
            finally
            {
                _exclusive.Release();
            }
        }

        #endregion

        #region Business Logic

        private async Task SetPriceAsync(string groupName, decimal price)
        {
            var column = Controller.Columns.FirstOrDefault(c => c.GroupName == groupName);
            if (column == null) return;

            // price = XXXX (BCD, LSD first), например: 12.34 → 1234 (масштабирование выполняется в парсере)
            await ExecuteCommandAsync(Command.ChangePrice, column.Address, column.Nozzle, price); // Level 1
        }

        private async Task StartRefuelingAsync(string groupName, decimal amount, bool bySum)
        {
            var column = Controller.Columns.FirstOrDefault(c => c.GroupName == groupName);
            if (column == null) return;

            // TWOTP: команда '2' → Preset Data → Volume или Money
            int presetType = bySum ? 2 : 1; // 2 = money, 1 = volume
            var scaled = bySum ? (int)(amount * 100) : (int)(amount * 100); // volume: в сотых, money: в центах

            await ExecuteCommandAsync(Command.SendData, Address, column.Nozzle, amount, expectedLength: 1, bySum: bySum);
            await Task.Delay(100, CancellationToken.None);
            //await ExecuteCommandAsync(Command.Authorize, Address, 0); // команда '1'
        }

        private async Task CompleteRefuelingAsync()
        {
            // TWOTP: Pump Stop команда '3'
            await ExecuteCommandAsync(Command.StopFueling, Address, 0);
        }

        private async Task ChangeControlModeAsync(string groupName, bool isProgramMode)
        {
            //try
            //{
            //    var column = GetColumnByGroupName(groupName);
            //    if (column is null)
            //    {
            //        _logger.Warning("Колонка {GroupName} не найдена", groupName);
            //        return;
            //    }

            //    var cmd = isProgramMode ? Command.ProgramControlMode : Command.KeyboardControlMode;
            //    await ExecuteCommandAsync(cmd, Address, column.LanfengAddress);
            //}
            //catch (Exception e)
            //{
            //    _logger.Error(e, e.Message);
            //}
        }

        private async Task StopFuelingAsync(string groupName)
        {
            //try
            //{
            //    var column = GetColumnByGroupName(groupName);

            //    if (column is null)
            //    {
            //        _logger.Warning("Колонка {GroupName} не найдена", groupName);
            //        return;
            //    }

            //    await ExecuteCommandAsync(Command.StopFueling, Address, column.LanfengAddress);
            //}
            //catch (Exception e)
            //{
            //    _logger.Error(e, e.Message);
            //}
        }

        private async Task ResumeFuelingAsync(string groupName)
        {
            //try
            //{
            //    var column = GetColumnByGroupName(groupName);

            //    if (column is null)
            //    {
            //        _logger.Warning("Колонка {GroupName} не найдена", groupName);
            //        return;
            //    }
            //    await ExecuteCommandAsync(Command.ContinueFueling, Address, column.LanfengAddress);
            //}
            //catch (Exception e)
            //{
            //    _logger.Error(e, e.Message);
            //}
        }

        private async Task GetStatusByAddressAsync(string groupName)
        {
            //try
            //{
            //    var column = GetColumnByGroupName(groupName);

            //    if (column is null)
            //    {
            //        _logger.Warning("Колонка {GroupName} не найдена", groupName);
            //        return;
            //    }
            //    await ExecuteCommandAsync(Command.Status, Address, column.LanfengAddress);
            //}
            //catch (Exception e)
            //{
            //    _logger.Error(e, e.Message);
            //}
        }

        #endregion

        #region Polling Control

        private async Task StartPollingAsync(CancellationToken token)
        {
            lock (_pollLock)
            {
                if (_pollingEnabled) return;
            }

            try
            {
                lock (_pollLock)
                {
                    _pollingEnabled = true;
                }

                _pollingTask = OnTickAsync(token);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Не удалось запустить TWOTP polling");
                lock (_pollLock) _pollingEnabled = false;
                if (_lease != null)
                {
                    await _lease.DisposeAsync();
                    _lease = null;
                }
            }
        }

        private async Task StopPollingAsync()
        {
            Task? toAwait = null;
            lock (_pollLock)
            {
                if (!_pollingEnabled) return;
                _pollingEnabled = false;
                toAwait = _pollingTask;
            }

            if (toAwait != null)
                await toAwait;

            if (_lease != null)
            {
                await _lease.DisposeAsync();
                _lease = null;
            }
            _sharedSerialPortService = null!;

            try
            {
                //await _portManager.CloseIfIdleAsync(key);
            }
            catch { }
        }

        #endregion

        #region Hub

        private void RegisterHubConnectionHandlers()
        {
            if (_hubHandlersRegistered || _hub is null)
                return;

            _hub.Reconnecting += OnHubReconnecting;
            _hub.Reconnected += OnHubReconnected;
            _hub.Closed += OnHubClosed;
            _hubHandlersRegistered = true;
        }

        private Task OnHubReconnecting(Exception? error)
        {
            _logger.Warning("Потеряно соединение с сервером: {Message}", error?.Message ?? "unknown");
            return Task.CompletedTask;
        }

        private async Task OnHubReconnected(string? connectionId)
        {
            _logger.Information("Переподключен с сервером. ConnectionId={ConnectionId}", connectionId);
            try
            {
                await JoinWorkerGroupsAsync();
                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Не удалось повторно присоединиться к группам после переподключения");
            }
        }

        private Task OnHubClosed(Exception? error)
        {
            _logger.Error(error, "Соединение с SignalR было закрыто");
            return RestartHubConnectionLoopAsync();
        }

        private Task RestartHubConnectionLoopAsync()
        {
            if (_hub is null)
                return Task.CompletedTask;

            if (Interlocked.CompareExchange(ref _hubRestartLoop, 1, 0) != 0)
                return Task.CompletedTask;

            return Task.Run(async () =>
            {
                try
                {
                    while (_hub.State != HubConnectionState.Connected)
                    {
                        try
                        {
                            await _hub.StartAsync();
                            await JoinWorkerGroupsAsync();
                            await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);
                            break;
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex, "Не удалось переподключиться к SignalR, повтор через 5 секунд");
                            await Task.Delay(TimeSpan.FromSeconds(5));
                        }
                    }
                }
                finally
                {
                    Interlocked.Exchange(ref _hubRestartLoop, 0);
                }
            });
        }

        private async Task JoinWorkerGroupsAsync()
        {
            if (_hub is null || Controller?.Columns is null)
                return;

            // Ждем, пока соединение станет активным
            var timeout = TimeSpan.FromSeconds(10);
            var stopwatch = Stopwatch.StartNew();

            while (_hub.State != HubConnectionState.Connected && stopwatch.Elapsed < timeout)
            {
                await Task.Delay(100);
            }

            if (_hub.State != HubConnectionState.Connected)
                throw new InvalidOperationException("Не удалось подключиться к SignalR за отведенное время");

            foreach (var item in Controller.Columns)
            {
                if (string.IsNullOrWhiteSpace(item.GroupName))
                {
                    item.GroupName = $"{Controller.Name}/{item.Name}";
                }

                await _hub.InvokeAsync("JoinController", item.GroupName, true);
            }
        }

        private async Task BroadcastWorkerAvailabilityAsync(bool isAvailable, string? reason = null, bool force = false)
        {
            var sanitizedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            if (!force &&
                _hardwareAvailable == isAvailable &&
                string.Equals(_lastAvailabilityReason ?? string.Empty, sanitizedReason ?? string.Empty, StringComparison.Ordinal))
            {
                return;
            }

            _hardwareAvailable = isAvailable;
            _lastAvailabilityReason = sanitizedReason;

            if (_hub is null || _hub.State != HubConnectionState.Connected)
                return;

            if (Controller?.Columns is null)
                return;

            var groups = Controller.Columns
                .Where(c => !string.IsNullOrWhiteSpace(c.GroupName))
                .Select(c => c.GroupName!);

            var tasks = groups.Select(group => SendAvailabilityAsync(group, isAvailable, sanitizedReason));
            await Task.WhenAll(tasks);
        }

        private async Task SendAvailabilityAsync(string groupName, bool isAvailable, string? reason)
        {
            try
            {
                var report = new WorkerAvailabilityReport
                {
                    GroupName = groupName,
                    IsAvailable = isAvailable,
                    Reason = reason
                };
                await _hub.InvokeAsync("ReportWorkerAvailability", report);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Не удалось отправить состояние worker для {Group}", groupName);
            }
        }

        private static bool IsCriticalSerialException(Exception ex) =>
            ex is TimeoutException || ex is IOException || ex is InvalidOperationException || ex is UnauthorizedAccessException;

        #endregion

        #region Logging

        private void CreateLogger()
        {
            var logRoot = Path.Combine(AppContext.BaseDirectory, "logs", "trk");
            Directory.CreateDirectory(logRoot);

            string safeController = Sanitize(Controller.Name);
            string safePort = Sanitize(Controller.ComPort);
            string controllerId = Controller.Id == Guid.Empty ? "noid" : Controller.Id.ToString("N");
            string fileName = $"TRK_Gilbarco_{safeController}_{safePort}_{controllerId}_{Address}.log";
            string path = Path.Combine(logRoot, fileName);

            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithProperty("Controller", Controller.Name)
                .Enrich.WithProperty("Address", Address)
                .Enrich.WithProperty("ComPort", Controller.ComPort)
                .WriteTo.File(
                    path: path,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            _logger.Information("Инициализация Gilbarco TWOTP для {Controller}/{Address}", Controller.Name, Address);
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "UNNAMED";
            s = Regex.Replace(s, @"[^\w\-\.\(\) ]+", "_");
            return s.Length > 80 ? s[..80] : s;
        }

        #endregion
    }
}

using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace KIT.GasStation.Emulator
{
    public sealed class EmulatorFuelDispenser : FuelDispenserServiceBase
    {
        #region Private Members

        private HubConnection _hub;
        private Task _pollingTask;
        private readonly object _pollLock = new();
        private volatile bool _pollingEnabled;
        private bool _hubHandlersRegistered;
        private volatile bool _hardwareAvailable = true;
        private string? _lastAvailabilityReason;
        private int _hubRestartLoop;
        private readonly StatusResponse _response = new() { Status = NozzleStatus.Ready };
        private CancellationToken _token;
        private readonly ConcurrentDictionary<string, (CancellationTokenSource cts, Task task)> _fuelingJobs = new();

        #endregion

        #region Constructors

        public EmulatorFuelDispenser(Controller controller,
            ISharedSerialPortService sharedSerialPortService,
            IHubClient hubClient) : base(controller, sharedSerialPortService, hubClient)
        {
            _hubClient = hubClient;
            _sharedSerialPortService = sharedSerialPortService;

            CreateLogger();
        }

        #endregion

        #region Override Voids

        protected override async Task OnTickAsync()
        {
            _logger.Information("ТРК Эмулятор запущена");

            var column = Columns.First();

            while (!_token.IsCancellationRequested && _pollingEnabled)
            {
                try
                {
                    // Ждем, если опрос приостановлен
                    _pollingResumedEvent.Wait(_token);

                    _response.GroupName = column.GroupName;
                    _response.Status = NozzleStatus.Ready;

                    await _hub.InvokeAsync("PublishStatus", _response, cancellationToken: _token);
                }
                catch (OperationCanceledException ex) when (ex.CancellationToken == _token)
                {
                    _logger.Information("Опрос ТРК Эмулятор отменён: {Message}", ex.Message);
                    break;
                }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message, e.StackTrace);
                    await Task.Delay(1000, _token);
                }
                if (!_pollingEnabled) break;
            }
        }

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            try
            {
                _token = token;

                _logger.Information("Начало инициализации ТРК {Id}. Состояние HubConnection: {State}", Controller.Id, _hub?.State.ToString() ?? "null");

                _hub = _hubClient.Connection;
                RegisterHubConnectionHandlers();

                _logger.Debug("EnsureStartedAsync вызван. Текущее состояние: {State}", _hub.State);

                _hub.On<StartPollingCommand>("StartPolling", async command =>
                {
                    await ExecuteHubCommandAsync(command.CommandId, command.GroupName, StartPollingAsync);
                });

                _hub.On<StopPollingCommand>("StopPolling", async command =>
                {
                    await ExecuteHubCommandAsync(command.CommandId, command.GroupName, StopPollingAsync);
                });

                _hub.On<Guid, Dictionary<string, decimal>>("SetPricesAsync", async (commandId, prices) =>
                {
                    var groupName = prices.Keys.FirstOrDefault() ?? string.Empty;
                    await ExecuteHubCommandAsync(commandId, groupName, async () => 
                        await SetPricesAsync(prices));
                });

                _hub.On<Guid, string, decimal>("SetPriceAsync", async (commandId, groupName, price) =>
                {
                    await ExecuteHubCommandAsync(commandId, groupName, async () => 
                        await SetPriceAsync(groupName, price));
                });

                _hub.On<Guid, string>("InitializeConfigurationAsync", async (commandId, groupName) =>
                {
                    await ExecuteHubCommandAsync(commandId, groupName,
                        () => Task.CompletedTask);
                });

                _hub.On<string, decimal, bool>("StartFuelingAsync", async (groupName, sum, bySum) =>
                {
                    await StartFuelingAsync(groupName, sum, bySum);
                });

                _hub.On<string>("CompleteFuelingAsync", async (groupName) =>
                {
                    await CompleteFuelingAsync(groupName);
                });

                _hub.On<string, bool>("ChangeControlModeAsync", async (groupName, isProgramMode) =>
                {
                    //await ChangeControlModeAsync(groupName, isProgramMode);
                });

                _hub.On<string>("StopFuelingAsync", async (groupName) =>
                {
                    await StopFuelingAsync(groupName);
                });

                _hub.On<string, decimal>("ResumeFuelingAsync", async (groupName, sum) =>
                {
                    await ResumeFuelingAsync(groupName, sum);
                });

                _hub.On<string>("GetStatusByAddressAsync", async (groupName) =>
                {
                    //await GetStatusByAddressAsync(groupName);
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

                _hub.On<Guid, string>("GetCountersAsync", async (commandId, groupName) =>
                {
                    await ExecuteHubCommandAsync(commandId, groupName, () => Task.CompletedTask);
                });

                _hub.On<Guid, string>("GetCounterAsync", async (commandId, groupName) =>
                {
                    await ExecuteHubCommandAsync(commandId, groupName, () => Task.CompletedTask);
                });

                // 1. СНАЧАЛА запускаем SignalR (синхронно)
                await _hubClient.EnsureStartedAsync(token);

                // 3. Присоединяемся к группам
                await JoinWorkerGroupsAsync();
                await BroadcastWorkerAvailabilityAsync(_hardwareAvailable, _lastAvailabilityReason, force: true);

            }
            catch (Exception e)
            {
                _logger.Error(e, "Ошибка в OnOpenAsync: {Message}", e.Message);
                throw;
            }
        }

        #endregion

        #region Private Voids

        private async Task StartPollingAsync()
        {
            if (_pollingTask == null || _pollingTask.IsCompleted)
            {
                lock (_pollLock)
                {
                    if (_pollingEnabled) return;      // уже запущено
                }

                try
                {
                    // 2) Только после успешного Acquire включаем флаг
                    lock (_pollLock)
                    {
                        _pollingEnabled = true;
                    }

                    // 3) Стартуем цикл опроса (lease остаётся жить в поле)
                    _pollingTask = OnTickAsync();
                    await BroadcastWorkerAvailabilityAsync(true, "Polling started");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Не удалось стартовать polling");
                    lock (_pollLock) { _pollingEnabled = false; }
                    await BroadcastWorkerAvailabilityAsync(false);
                }
            }

            var column = Columns.First();

            _response.GroupName = column.GroupName;

            await _hub.InvokeAsync("PublishStatus", _response, cancellationToken: _token);
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
        }

        private async Task StartFuelingAsync(string groupName, decimal value, bool bySum)
        {
            var cts = new CancellationTokenSource();

            var task = Task.Run(async () =>
            {
                try
                {
                    await StartFuelingLoopAsync(groupName, value, bySum, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.Information("Fueling cancelled: {GroupName}", groupName);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Fueling crashed: {GroupName}", groupName);
                }
                finally
                {
                    // убираем запись, только если это она (защита от гонок)
                    if (_fuelingJobs.TryGetValue(groupName, out var cur) && cur.cts == cts)
                        _fuelingJobs.TryRemove(groupName, out _);

                    cts.Dispose();
                }
            });

            _fuelingJobs[groupName] = (cts, task);
        }

        private async Task StartFuelingLoopAsync(string groupName, decimal value, bool bySum, CancellationToken token)
        {
            try
            {
                await PausePollingAsync(); // если нужно — тут ок

                var column = GetColumnByGroupName(groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                decimal sum = bySum ? value : Math.Round(value * column.Price, 2);
                decimal quantity = bySum ? Math.Round(value / column.Price, 3) : value;

                var rnd = new Random();
                decimal receivedQuantity = 0;
                _response.GroupName = groupName;
                _response.Status = NozzleStatus.WaitingRemoved;
                await _hub.InvokeAsync("PublishStatus", _response, cancellationToken: token);

                await Task.Delay(1000, token);

                _response.Status = NozzleStatus.PumpWorking;
                await _hub.InvokeAsync("PublishStatus", _response, cancellationToken: token);

                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    decimal step = (decimal)rnd.NextDouble() * 0.5m;
                    receivedQuantity += step;
                    decimal receivedSum = Math.Round(receivedQuantity * column.Price, 2);
                    if (receivedQuantity > quantity)
                    {
                        receivedQuantity = quantity;
                        receivedSum = sum;
                    }

                    var fuelingResponse = new FuelingResponse
                    {
                        GroupName = groupName,
                        Quantity = receivedQuantity,
                        Sum = receivedSum
                    };

                    await _hub.InvokeAsync("OnFuelingAsync", fuelingResponse, cancellationToken: token);

                    if (receivedQuantity >= quantity)
                        break;

                    await Task.Delay(300, token);
                }

                await Task.Delay(1000, token);
                await _hub.InvokeAsync("OnCompletedFuelingAsync", groupName, cancellationToken: token);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        private void StopFueling(string groupName)
        {
            if (_fuelingJobs.TryRemove(groupName, out var job))
            {
                job.cts.Cancel();
                // Dispose будет в finally у таски — так безопаснее
            }
        }

        private async Task StopFuelingAsync(string groupName)
        {
            try
            {
                await PausePollingAsync();

                StopFueling(groupName);

                var column = GetColumnByGroupName(groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }
                _response.GroupName = groupName;
                _response.Status = NozzleStatus.WaitingRemoved;
                await _hub.InvokeAsync("PublishStatus", _response, cancellationToken: _token);

                await Task.Delay(500, _token);

                await _hub.InvokeAsync("OnWaitingAsync", groupName, cancellationToken: _token);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        private async Task ResumeFuelingAsync(string groupName, decimal sum)
        {
            var cts = new CancellationTokenSource();

            var task = Task.Run(async () =>
            {
                try
                {
                    await StartFuelingLoopAsync(groupName, sum, true, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _logger.Information("Fueling cancelled: {GroupName}", groupName);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Fueling crashed: {GroupName}", groupName);
                }
                finally
                {
                    // убираем запись, только если это она (защита от гонок)
                    if (_fuelingJobs.TryGetValue(groupName, out var cur) && cur.cts == cts)
                        _fuelingJobs.TryRemove(groupName, out _);

                    cts.Dispose();
                }
            });

            _fuelingJobs[groupName] = (cts, task);
        }

        private async Task SetPricesAsync(Dictionary<string, decimal> prices)
        {
            try
            {
                await PausePollingAsync();

                foreach (var (group, price) in prices)
                {
                    var column = GetColumnByGroupName(group);

                    if (column is null)
                    {
                        _logger.Warning("Колонка {GroupName} не найдена", group);
                        return;
                    }

                    column.Price = price;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        private async Task SetPriceAsync(string group, decimal price)
        {
            try
            {
                await PausePollingAsync();

                var column = GetColumnByGroupName(group);

                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", group);
                    return;
                }

                column.Price = price;
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
            finally
            {
                await ResumePollingAsync();
            }
        }

        private async Task CompleteFuelingAsync(string groupName)
        {
            await _hub.InvokeAsync("OnCompletedFuelingAsync", groupName, cancellationToken: _token);
        }

        #endregion

        #region Helpers

        private Column? GetColumnByGroupName(string groupName)
        {
            return Columns.FirstOrDefault(c => c.GroupName == groupName);
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

        #region Hub

        private async Task ExecuteHubCommandAsync(Guid commandId, string groupName, Func<Task> action)
        {
            Exception? error = null;
            try
            {
                await action();
            }
            catch (Exception ex)
            {
                error = ex;
                _logger.Error(ex, "Ошибка выполнения команды {CommandId} для {Group}", commandId, groupName);
            }

            await ReportCommandCompletedAsync(commandId, groupName, error);
        }

        private async Task ReportCommandCompletedAsync(Guid commandId, string groupName, Exception? error)
        {
            if (_hub is null || _hub.State != HubConnectionState.Connected)
                return;

            var completion = new CommandCompletion
            {
                CommandId = commandId,
                GroupName = groupName,
                IsSuccess = error is null,
                ErrorMessage = error?.Message
            };

            try
            {
                await _hub.InvokeAsync("ReportCommandCompleted", completion);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Не удалось отправить подтверждение команды {CommandId}", commandId);
            }
        }

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

        #region Logs

        private void CreateLogger()
        {
            // создаём общую папку для логов ТРК
            var logRoot = Path.Combine(AppContext.BaseDirectory, "logs", "trk");
            Directory.CreateDirectory(logRoot);

            // безопасное имя файла (уникальное для экземпляра)
            string safeController = Sanitize(Controller.Name);
            string safePort = Sanitize(Controller.ComPort);
            string controllerId = Controller.Id == Guid.Empty ? "noid" : Controller.Id.ToString("N");
            string fileName = $"TRK_{Controller.Type}_{safeController}_{safePort}_{controllerId}_{Address}.log";
            string path = Path.Combine(logRoot, fileName);

            // отдельный Serilog для файла инстанса
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
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            _logger.Information("Инициализация Emulator для {Controller}/{Address} на порту {Port}",
            Controller.Name, Address, Controller.ComPort);
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "UNNAMED";
            s = s.Trim();
            s = Regex.Replace(s, @"[^\w\-\.\(\) ]+", "_"); // заменяем недопустимые символы
            return s.Length > 80 ? s[..80] : s;
        }

        #endregion
    }
}

using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Hubs;
using KIT.GasStation.FuelDispenser.Models;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using KIT.GasStation.TechnoProjekt.Utilities;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace KIT.GasStation.Technoproject
{
    public sealed class TechnoprojectFuelDispenser : FuelDispenserServiceBase
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
        private const int _frameLen = 23;
        private ILogger _logger;
        private CancellationToken _token;

        #endregion

        #region Constructors

        public TechnoprojectFuelDispenser(Controller controller,
            int address,
            ISharedSerialPortService sharedSerialPortService,
            IHubClient hubClient)
            : base(controller, address, sharedSerialPortService, hubClient)
        {
            _hubClient = hubClient;

            CreateLogger();
        }

        #endregion

        #region Protected

        protected override async Task OnOpenAsync(CancellationToken token)
        {
            try
            {
                _hub = _hubClient.Connection;

                _hub.On<StartPollingCommand>("StartPolling", async e =>
                {
                    await StartPollingAsync(token);
                });

                _hub.On<StopPollingCommand>("StopPolling", async e =>
                {
                    await StopPollingAsync();
                });

                _hub.On<string, decimal>("SetPriceAsync", async (groupName, price) =>
                {
                    await SetPriceAsync(groupName, price);
                });

                _hub.On<string, decimal, bool>("StartRefuelingAsync", async (groupName, sum, bySum) =>
                {
                    await StartRefuelingAsync(groupName, sum, bySum);
                });

                _hub.On<string>("CompleteRefuelingAsync", async (groupName) =>
                {
                    await CompleteRefuelingAsync(groupName);
                });

                _hub.On<string>("GetCountersAsync", async (groupName) =>
                {
                    var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                    if (column is not null)
                    {
                        await ExecuteCommandAsync(Command.SetCounters, Address, column.Address);
                    }
                });

                await _hubClient.EnsureStartedAsync(token);

                foreach (var item in Controller.Columns)
                {
                    string groupName = $"{Controller.Name}/{item.Name}";
                    await _hub.InvokeAsync("JoinController", groupName);
                    item.GroupName = groupName;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
        }

        protected override async Task OnTickAsync()
        {
            _logger.Information("ТРК Technoproject запущена, используется порт {Port}", Controller.ComPort);

            // Инициализируем, если нужно прочитать параметры
            await ExecuteCommandAsync(Command.ReadParams, Address, 0);

            // Стартовый опрос состояния
            await ExecuteCommandAsync(Command.Status, Address, 0);

            while (!_token.IsCancellationRequested && _pollingEnabled)
            {
                try
                {
                    await ExecuteCommandAsync(Command.Status, Address, 0, null, expectedLength: _frameLen, ct: _token);
                }
                catch (OperationCanceledException) { }
                catch (Exception e)
                {
                    _logger.Error(e, e.Message);
                }

                if (!_pollingEnabled) break;

                // Небольшая задержка между опросами — можно настроить
                await Task.Delay(300, _token);
            }
        }

        protected override Task OnCloseAsync() => Task.CompletedTask;

        public override async ValueTask DisposeAsync()
        {
            (_logger as IDisposable)?.Dispose();
            await base.DisposeAsync();
        }

        #endregion

        #region Private Methods

        private async Task ExecuteCommandAsync(
            Command cmd,
            int controllerAddress,
            int nozzleMask,
            decimal? value = null,
            int expectedLength = _frameLen,
            int writeToReadDelayMs = 0,
            int readTimeoutMs = 3000,
            int maxRetries = 2,
            CancellationToken ct = default)
        {
            if (_sharedSerialPortService is null)
                throw new InvalidOperationException("Последовательный порт еще не получен.");

            await _pauseGate.WaitAsync(ct);
            await _exclusive.WaitAsync(ct);
            try
            {
                var frame = ProtocolParser.BuildRequest(cmd, controllerAddress, nozzleMask, value);
                _logger.Information("[Tx] {Tx}", BitConverter.ToString(frame));

                var rx = await _sharedSerialPortService.WriteReadAsync(
                    frame,
                    expectedRxLength: expectedLength,
                    writeToReadDelayMs: writeToReadDelayMs,
                    readTimeoutMs: readTimeoutMs,
                    maxRetries: maxRetries,
                    ct: ct);

                //var resp = ProtocolParser.ParseResponse(rx);

                //if (resp is not null && resp.IsValid)
                //{
                //    Column? column = null;

                //    // Пытаемся найти колонку по адресу TRK_No (resp.Address)
                //    if (Columns is not null)
                //    {
                //        column = Columns.FirstOrDefault(c => c.Address == resp.Address) ?? Columns.FirstOrDefault();
                //    }

                //    if (column is not null)
                //    {
                //        resp.Group = column.GroupName;
                //        await _hub.InvokeAsync("PublishStatus", resp, column.GroupName);
                //    }

                //    // Обработка поднятия пистолета — в протоколе адрес и статус в статус-байтах
                //    await HandleColumnLiftedAsync(resp.Data);
                //}

                _logger.Information("[Rx] {Rx}", BitConverter.ToString(rx));
            }
            finally
            {
                _exclusive.Release();
            }
        }

        private async Task ExecuteSequenceAsync(
            Func<Func<Command, int, int, decimal?, Task<byte[]>>, Task> body,
            CancellationToken ct = default)
        {
            if (_sharedSerialPortService is null)
                throw new InvalidOperationException("Последовательный порт еще не получен.");

            await _pauseGate.WaitAsync(ct);
            await _exclusive.WaitAsync(ct);
            try
            {
                async Task<byte[]> send(Command c, int addr, int mask, decimal? val)
                {
                    var frame = ProtocolParser.BuildRequest(c, addr, mask, val);
                    _logger.Information("[Tx] {Tx}", BitConverter.ToString(frame));
                    var rx = await _sharedSerialPortService.WriteReadAsync(frame, expectedRxLength: _frameLen, writeToReadDelayMs: 0, readTimeoutMs: 3000, maxRetries: 2, ct: ct);
                    _logger.Information("[Rx] {Rx}", BitConverter.ToString(rx));
                    return rx;
                }

                await body(send);
            }
            finally
            {
                _exclusive.Release();
            }
        }

        private async Task SetPriceAsync(string groupName, decimal price)
        {
            try
            {
                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                // Для Technoproject используем команду Setup для установки цены (см. протокол)
                await ExecuteCommandAsync(Command.Setup, Address, column.Address, price);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
        }

        private async Task StartRefuelingAsync(string groupName, decimal sumOrQty, bool bySum)
        {
            try
            {
                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                // Для задания дозы сначала даём команду SetDose (9), затем старт (5)
                await ExecuteCommandAsync(Command.StartFuelingQuantity, Address, column.Address, sumOrQty, expectedLength: _frameLen);
                // Небольшая пауза между командами
                await Task.Delay(150);
                await ExecuteCommandAsync(Command.Start, Address, column.Address, null, expectedLength: _frameLen);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
            }
        }

        private async Task CompleteRefuelingAsync(string groupName)
        {
            try
            {
                var column = Columns.FirstOrDefault(c => c.GroupName == groupName);
                if (column is null)
                {
                    _logger.Warning("Колонка {GroupName} не найдена", groupName);
                    return;
                }

                // Для завершения заправки используем Stop (6) или Reset (7) в зависимости от логики
                await ExecuteCommandAsync(Command.StopFueling, Address, column.Address);
            }
            catch (Exception e)
            {
                _logger.Error(e, e.Message);
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

            if (toAwait is not null)
            {
                try { await toAwait; } catch { }
            }

            if (_lease is not null)
            {
                await _lease.DisposeAsync();
                _lease = null;
            }
            _sharedSerialPortService = null!;

            
        }

        private async Task StartPollingAsync(CancellationToken token)
        {
            if (_pollingTask == null || _pollingTask.IsCompleted)
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

                    _pollingTask = OnTickAsync();
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Не удалось стартовать polling");
                    lock (_pollLock) { _pollingEnabled = false; }

                    if (_lease is not null)
                    {
                        await _lease.DisposeAsync();
                        _lease = null;
                    }
                    _sharedSerialPortService = null!;
                }
            }
        }

        private async Task HandleColumnLiftedAsync(byte[] response)
        {
            if (response is null || response.Length < _frameLen) return;

            // В протоколе ТехноПроект нет явного low-nibble для пистолета, но есть байты статуса (17..18).
            // Попробуем прочитать младший ниббл байта 17 как индекс пистолета (если применимо)
            int liftedOrdinal = response[17] & 0x0F;

            foreach (var column in Columns)
            {
                bool isLifted = column.Address == liftedOrdinal;
                if (column.IsLifted != isLifted)
                {
                    column.IsLifted = isLifted;
                    await _hub.InvokeAsync("ColumnLiftedChanged", column.GroupName, isLifted);
                }
            }
        }

        #endregion

        #region Logs

        private void CreateLogger()
        {
            var logRoot = Path.Combine(AppContext.BaseDirectory, "logs", "trk");
            Directory.CreateDirectory(logRoot);

            string safeController = Sanitize(Controller.Name);
            string safePort = Sanitize(Controller.ComPort);
            string controllerId = Controller.Id == Guid.Empty ? "noid" : Controller.Id.ToString("N");
            string fileName = $"TRK_Technoproject_{safeController}_{safePort}_{controllerId}_{Address}.log";
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
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            _logger.Information("Инициализация Technoproject для {Controller}/{Address} на порту {Port}",
                Controller.Name, Address, Controller.ComPort);
        }

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "UNNAMED";
            s = s.Trim();
            s = Regex.Replace(s, @"[^\w\-\.\(\) ]+", "_");
            return s.Length > 80 ? s[..80] : s;
        }

        #endregion
    }
}
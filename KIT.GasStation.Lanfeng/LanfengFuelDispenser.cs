using KIT.GasStation.Domain.Models;
using KIT.GasStation.FuelDispenser.Commands;
using KIT.GasStation.FuelDispenser.Services;
using KIT.GasStation.FuelDispenser.Services.Factories;
using KIT.GasStation.HardwareConfigurations.Models;
using KIT.GasStation.HardwareConfigurations.Services;
using Serilog;
using System.ComponentModel;
using System.Reflection;

namespace KIT.GasStation.Lanfeng
{
    /// <summary>
    /// Сервис для работы с колонкой Lanfeng через COM-порт.
    /// Прослушивает статусы и обрабатывает команды.
    /// </summary>
    public class LanfengFuelDispenser : IFuelDispenserService
    {
        #region Private Members

        private readonly IPortManager _portManager;
        private readonly IHardwareConfigurationService _hardwareConfigurationService;
        private readonly IProtocolParser _parser;
        private ISharedSerialPortService _portService;
        private int _controllerAddress;
        private readonly List<Nozzle> _nozzles;
        private ILogger _logger;
        private LanfengControllerType _lanfengControllerType;

        private CancellationTokenSource _statusCts;
        private Task _statusTask;

        #endregion

        #region Event Actions

        /// <inheritdoc/>
        //public event Action<Guid, ColumnStatus> OnStatusChanged;

        /// <inheritdoc/>
        //public event Action<Guid, decimal> OnCounterReceived;

        /// <inheritdoc/>
        public event Action<Guid> OnColumnLifted;

        /// <inheritdoc/>
        public event Action OnColumnLowered;

        /// <inheritdoc/>
        //public event Action<Guid, decimal, decimal> OnStartedFilling;

        /// <inheritdoc/>
        public event Action<int> OnWaitingRemoved;

        /// <inheritdoc/>
        public event Action<int> OnCompletedFilling;

        /// <inheritdoc/>
        public event Action OnConnectionLost;

        #endregion

        #region Public Proeprties

        /// <inheritdoc/>
        public string DispenserName => Assembly.GetExecutingAssembly().GetName().Name ?? "Unknown";

        /// <inheritdoc/>
        public string Version => Assembly.GetExecutingAssembly()
                                         .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                                         .InformationalVersion
                                     ?? Assembly.GetExecutingAssembly()
                                                .GetName()
                                                .Version?
                                                .ToString()
                                     ?? "Версия не найдена";

        #endregion

        #region Constructors

        public LanfengFuelDispenser(IPortManager portManager,
            IHardwareConfigurationService hardwareConfigurationService,
            IProtocolParserFactory protocolParserFactory,
            IEnumerable<Nozzle>? nozzles)
        {
            _portManager = portManager;
            _hardwareConfigurationService = hardwareConfigurationService;
            _nozzles = nozzles?.ToList() ?? new List<Nozzle>();
            _parser = protocolParserFactory.CreateIProtocolParser(ControllerType.Lanfeng, _nozzles);
        }

        #endregion

        #region Public Commands

        /// <inheritdoc/>
        public async Task StartStatusPolling(int intervalMs = 200)
        {
            if (_statusCts != null)
                throw new InvalidOperationException("Пуллинг уже запущен");

            if (_nozzles is { Count: > 0 })
            {
                foreach (var item in _nozzles)
                {
                    Column? col = await _hardwareConfigurationService.GetColumnByIdAsync(item.ColumnId);

                    if (col != null)
                    {
                        item.Number = col.Nozzle;
                    }
                }
            }
            else
            {
                return;
            }

            Nozzle nozzle = _nozzles[0];

            Column? column = await _hardwareConfigurationService.GetColumnByIdAsync(nozzle.ColumnId);

            if (column == null)
            {
                _logger.Error($"Колонка с идентификатором {nozzle.ColumnId} не найдена.");
                return;
            }

            // Устанавливаем адрес
            _controllerAddress = column.Address;

            if (column.Controller == null)
            {
                _logger.Error($"Колонка с идентификатором {nozzle.ColumnId} не содержит информации о контроллере.");
                return;
            }

            _portService = await _portManager.GetPortServiceAsync(column.Controller.ComPort, column.Controller.BaudRate);

            await InitializeAsync(nozzle.Side);

            _statusCts = new CancellationTokenSource();
            _statusTask = Task.Run(() => StatusLoopAsync(intervalMs, _statusCts.Token));
        }

        public async Task Connect(string comPort, int baudRate)
        {
            try
            {
                _portService = await _portManager.GetPortServiceAsync(comPort, baudRate);
            }
            catch (Exception)
            {

            }
        }

        public async Task<NozzleStatus> CheckStatusAsync(Column column)
        {
            return NozzleStatus.Unknown;
        }

        public async Task SetPriceAsync(Nozzle nozzle, decimal? price = null)
        {

        }

        public async Task StartRefuelingSumAsync(Nozzle nozzle, decimal? sum = null)
        {

        }

        #endregion

        #region Private Voids

        private async Task StatusLoopAsync(int intervalMs, CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    foreach (var nozzle in _nozzles)
                    {
                        var request = _parser.BuildRequest(Command.Status, _controllerAddress, 0);

#if DEBUG

                        string asd = BitConverter.ToString(request);

#endif
                        var raw = await _portService.WriteReadAsync(request, expectedResponseLength: 14)
                                                      .ConfigureAwait(false);
                        var response = _parser.ParseResponse(raw, Command.Status);
                        
                        nozzle.Status  = response.Status;
                        nozzle.ReceivedQuantity = response.Quantity;
                        nozzle.ReceivedSum = response.Sum;
                        
                        //OnDataReceived?.Invoke(nozzle, response);
                    }
                    await Task.Delay(intervalMs, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
                // Ожидаемо при остановке
            }
            catch (Exception ex)
            {
                //OnConnectionLost?.Invoke(ex);
            }
        }

        private async Task InitializeAsync(int side)
        {
            try
            {
                InitLog(side);

                // Отправим статус
                var request = _parser.BuildRequest(Command.Status, _controllerAddress, 0);

                _logger.Information($"[Tx] {BitConverter.ToString(request)}");

                var raw = await _portService.WriteReadAsync(request, expectedResponseLength: 14)
                                              .ConfigureAwait(false);
                var response = _parser.ParseResponse(raw, Command.Status);

                if (response.IsValid)
                {
                    _logger.Information($"[Rx] {BitConverter.ToString(response.Data)}");

                    // Программное управление
                    await SwitchToProgramControlAsync();

                    // Получаем версию прошивки
                    await GetFirmwareVersionAsync();

                    // Инициализация по пистолетам
                    await InitializeByNozzlesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ошибка при инициализации колонки.");
            }
        }

        private async Task GetFirmwareVersionAsync()
        {
            // Проверяем версию прошивку
            var request = _parser.BuildRequest(Command.FirmwareVersion, _controllerAddress, 0);

            _logger.Information($"[Tx] {BitConverter.ToString(request)}");

            var raw = await _portService.WriteReadAsync(request, expectedResponseLength: 14)
                                      .ConfigureAwait(false);

            var response = _parser.ParseResponse(raw, Command.FirmwareVersion);

            _logger.Information($"[Rx] {BitConverter.ToString(response.Data)}");

            if (response.IsValid)
            {
                _lanfengControllerType = (LanfengControllerType)response.Data[3];
            }
        }

        private async Task SwitchToProgramControlAsync()
        {
            var request = _parser.BuildRequest(Command.ProgramControlMode, _controllerAddress, 0);

            _logger.Information($"[Tx] {BitConverter.ToString(request)}");

            var raw = await _portService.WriteReadAsync(request, expectedResponseLength: 14)
                                      .ConfigureAwait(false);

            var response = _parser.ParseResponse(raw, Command.ProgramControlMode);

            _logger.Information($"[Rx] {BitConverter.ToString(response.Data)}");
        }

        private async Task InitializeByNozzlesAsync()
        {
            foreach (var nozzle in _nozzles)
            {

            }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            //// 1. Остановить фоновый цикл
            //StopStatusLoop();
            //_statusLoopTask?.GetAwaiter().GetResult();

            //// 2. Освободить семафор и CTS
            //_statusLoopSemaphore?.Dispose();
            //_statusLoopCts?.Dispose();

            //// 3. Закрыть/освободить порт
            //_sharedSerialPortService?.Dispose();

            //_nozzles = null;
            //_columns = null;
        }

        #endregion

        #region Logs

        /// <summary>
        /// Инициализация логгера.
        /// </summary>
        private void InitLog(int side)
        {
            // 1. Создадим/убедимся, что существует папка logs
            var logsDir = Path.Combine(AppContext.BaseDirectory, "logs");
            Directory.CreateDirectory(logsDir);

            // 2. Формируем имя файла. Можно добавить время, 
            //    но обязательно без «:» (двоеточий). Например, yyyy-MM-dd_HH-mm-ss.
            var logFilePath = Path.Combine(logsDir, $"{nameof(LanfengFuelDispenser)}_{side}_{DateTime.Now:yyyyddMM}.log");

            // 3. Настраиваем Serilog
            _logger = new LoggerConfiguration()
                // Указываем минимальный уровень
                .MinimumLevel.Debug()
                // Пишем в файл с «дневным» ротационным интервалом
                .WriteTo.File(
                    path: logFilePath,
                    rollingInterval: RollingInterval.Day,
                    // Можно задать, сколько файлов хранить
                    retainedFileCountLimit: 7,
                    // Можно включить автопереход на новый файл при достижении лимита размера
                    rollOnFileSizeLimit: true
                )
                // При желании можно добавить вывод в консоль
                //.WriteTo.Console()
                .CreateLogger();

            // 4. Пробный лог на уровне Information
            _logger.Information($"---------------Ланфенг инициализирован. [{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")}]---------------");
        }

        #endregion

        #region PropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}

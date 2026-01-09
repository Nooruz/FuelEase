using Serilog;
using System.Diagnostics;
using System.IO.Ports;
using System.Text.RegularExpressions;

namespace KIT.GasStation.HardwareConfigurations.Services
{
    /// <summary>
    /// Реализация общего владельца порта с очередью I/O.
    /// </summary>
    public class SharedSerialPortService : ISharedSerialPortService
    {
        #region Private Members

        private readonly SemaphoreSlim _io = new(1, 1); // общий семафор на I/O (half-duplex)
        private SerialPort? _port;
        private volatile bool _isOpen;
        private PortKey _key;
        private SerialPortOptions? _options;
        private ILogger _logger;
        private readonly SemaphoreSlim _openLock = new(1, 1);

        #endregion

        #region Public Properties

        public bool IsOpen => _isOpen;
        public string PortName => _key.PortName;

        #endregion

        #region Constructors

        public SharedSerialPortService()
        {
            CreateLogger();
        }

        #endregion

        #region Public Voids

        /// <inheritdoc/>
        public async Task OpenAsync(PortKey key, SerialPortOptions options, CancellationToken ct)
        {
            await _openLock.WaitAsync(ct);
            try
            {
                // Идемпотентное открытие
                if (_isOpen && _port is not null && _port.IsOpen && _key.Equals(key))
                {
                    _logger.Debug("Port {PortName} is already open", key.PortName);
                    return;
                }

                await CloseAsync();
                _key = key;
                _options = options;

                await OpenWithRetriesAsync(key, options, ct);
            }
            finally
            {
                _openLock.Release();
            }
        }

        /// <inheritdoc/>
        public async Task CloseAsync()
        {
            _logger.Debug("Closing port {PortName}", _key.PortName);

            try
            {
                var p = _port;
                _port = null;
                _isOpen = false;

                if (p is not null)
                {
                    if (p.IsOpen)
                    {
                        p.DiscardInBuffer();
                        p.DiscardOutBuffer();
                        p.Close();
                        _logger.Information("Port {PortName} closed successfully", _key.PortName);
                    }
                    p.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error closing port {PortName}", _key.PortName);
                // Пробрасываем дальше, чтобы знать о проблеме
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<byte[]> WriteReadAsync(byte[] tx,
            int expectedRxLength,
            int writeToReadDelayMs = 50,
            int readTimeoutMs = 3000,
            int maxRetries = 2,
            CancellationToken ct = default)
        {
            var portLabel = _key.PortName ?? "<unset>";
            byte[]? result = null;

            _logger.Debug("Serial {Port}: waiting for I/O lock.", portLabel);
            await _io.WaitAsync(ct);

            try
            {
                _logger.Debug("Serial {Port}: acquired I/O lock.", portLabel);

                if (PortRequiresReopen())
                {
                    _logger.Warning("Serial {Port}: port requires reopen before I/O.", portLabel);
                    await RecoverPortAsync(ct);
                }

                // Используем Task.Run для вызова синхронного метода в фоновом потоке
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
                        _logger.Debug("Serial {Port}: starting operation (attempt {Attempt}/{MaxRetries})",
                            portLabel, attempt, maxRetries);

                        // Выполняем синхронную операцию в фоновом потоке
                        result = await Task.Run(() =>
                            WriteReadOnce(tx, expectedRxLength, writeToReadDelayMs,
                                readTimeoutMs, attempt, maxRetries, portLabel, ct), ct);

                        _logger.Debug("Serial {Port}: operation successful (attempt {Attempt})",
                            portLabel, attempt);
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.Warning("Serial {Port}: operation cancelled", portLabel);
                        throw;
                    }
                    catch (TimeoutException ex) when (attempt < maxRetries)
                    {
                        _logger.Warning(ex, "Serial {Port}: timeout (attempt {Attempt}/{MaxRetries})",
                            portLabel, attempt, maxRetries);

                        // Небольшая задержка перед повторной попыткой
                        await Task.Delay(100, ct);
                    }
                    catch (Exception ex) when (attempt < maxRetries && IsRecoverableException(ex))
                    {
                        _logger.Warning(ex, "Serial {Port}: recoverable error (attempt {Attempt}/{MaxRetries})",
                            portLabel, attempt, maxRetries);

                        await Task.Delay(TimeSpan.FromMilliseconds(100 * attempt), ct);
                    }
                }

                if (result == null)
                {
                    throw new IOException($"Не удалось выполнить операцию чтения/записи после {maxRetries} попыток");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Serial {Port}: I/O operation failed.", portLabel);
                throw;
            }
            finally
            {
                try
                {
                    _io.Release();
                    _logger.Debug("Serial {Port}: released I/O lock.", portLabel);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Serial {Port}: failed to release I/O lock!", portLabel);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
            _io.Dispose();
        }

        #endregion

        #region Private Helpers

        private async Task OpenWithRetriesAsync(PortKey key, SerialPortOptions options, CancellationToken ct)
        {
            var totalTimeout = TimeSpan.FromMilliseconds(Math.Max(0, options.OpenRetryTimeoutMs));
            var retryDelay = TimeSpan.FromMilliseconds(Math.Max(1, options.OpenRetryDelayMs));
            var sw = Stopwatch.StartNew();
            Exception? lastError = null;

            while (true)
            {
                try
                {
                    // Открытие делаем синхронным внутри Task.Run, чтобы не блокировать вызывающий поток.
                    await Task.Run(() => OpenPortOnce(key, options), ct);
                    return;
                }
                catch (Exception ex) when (IsPortUnavailableException(ex))
                {
                    lastError = ex;
                    if (sw.Elapsed >= totalTimeout)
                    {
                        throw new IOException($"Не удалось открыть порт {key.PortName} в течение отведённого времени.", ex);
                    }

                    var remaining = totalTimeout - sw.Elapsed;
                    var delay = remaining < retryDelay ? remaining : retryDelay;
                    if (delay <= TimeSpan.Zero)
                        delay = TimeSpan.FromMilliseconds(50);

                    await Task.Delay(delay, ct);
                }
            }
        }

        private void OpenPortOnce(PortKey key, SerialPortOptions options)
        {
            SerialPort? createdPort = null;
            try
            {
                var p = new SerialPort(key.PortName, key.BaudRate, key.Parity, key.DataBits, key.StopBits)
                {
                    ReadTimeout = options.ReadTimeoutMs,
                    WriteTimeout = options.WriteTimeoutMs,
                    RtsEnable = options.RtsEnable,
                    DtrEnable = options.DtrEnable
                };
                p.ReadBufferSize = Math.Max(p.ReadBufferSize, options.ReadBufferSize);
                p.WriteBufferSize = Math.Max(p.WriteBufferSize, options.WriteBufferSize);

                createdPort = p;
                p.Open();

                _port = p;
                _isOpen = true;
                createdPort = null; // ответственность передана _port
            }
            finally
            {
                if (createdPort is not null)
                {
                    try
                    {
                        createdPort.Dispose();
                    }
                    catch
                    {
                        // best effort cleanup
                    }
                }
            }
        }

        private static bool IsPortUnavailableException(Exception ex) =>
            ex is IOException || ex is UnauthorizedAccessException;

        private bool PortRequiresReopen()
        {
            try
            {
                if (!_isOpen || _port is null)
                    return true;

                if (!_port.IsOpen)
                    return true;

                // Дополнительная проверка: если порт отключен физически
                return IsPortDisconnected();
            }
            catch (InvalidOperationException)
            {
                // Порт в недопустимом состоянии
                return true;
            }
        }

        private bool IsPortDisconnected()
        {
            if (_port == null || !_port.IsOpen)
                return true;

            try
            {
                // Пытаемся получить свойства порта - если порт отключен, это вызовет исключение
                var portName = _port.PortName;
                var baudRate = _port.BaudRate;

                // Дополнительная проверка: пытаемся прочитать доступные байты
                // Если порт отключен, это может вызвать исключение
                var bytesToRead = _port.BytesToRead;
                return false;
            }
            catch (InvalidOperationException)
            {
                // Порт был отключен или находится в недопустимом состоянии
                return true;
            }
            catch (IOException)
            {
                // Ошибка ввода-вывода, вероятно порт отключен
                return true;
            }
        }

        private async Task RecoverPortAsync(CancellationToken ct)
        {
            if (_options is null)
            {
                _logger.Error("Serial port options are not set during recovery");
                throw new InvalidOperationException("Параметры последовательного порта не заданы.");
            }

            _logger.Warning("Recovering port {PortName}", _key.PortName);

            try
            {
                await CloseAsync();
                await OpenWithRetriesAsync(_key, _options, ct);
                _logger.Information("Port {PortName} recovered successfully", _key.PortName);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to recover port {PortName}", _key.PortName);
                throw;
            }
        }

        private void CreateLogger()
        {
            // создаём общую папку для логов ТРК
            var logRoot = Path.Combine(AppContext.BaseDirectory, "logs", "ports");
            Directory.CreateDirectory(logRoot);

            // безопасное имя файла (уникальное для экземпляра)
            string fileName = Sanitize($"Port_{new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day)}");
            string path = Path.Combine(logRoot, fileName + ".log");

            // отдельный Serilog для файла инстанса
            _logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: path,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();

            _logger.Information("Инициализация SharedSerialPort");
        }

        // sanitization для имени файла
        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "UNNAMED";
            s = s.Trim();
            s = Regex.Replace(s, @"[^\w\-\.\(\) ]+", "_"); // заменяем недопустимые символы
            return s.Length > 80 ? s[..80] : s;
        }

        private byte[] WriteReadOnce(byte[] tx, int expectedRxLength,
            int writeToReadDelayMs, int readTimeoutMs, int attempt, int maxRetries,
            string portLabel, CancellationToken ct)
        {
            // Устанавливаем таймауты
            _port.ReadTimeout = readTimeoutMs;
            _port.WriteTimeout = 1000; // 1 секунда на запись

            _logger.Debug("Serial {Port}: write {TxLength} bytes (attempt {Attempt}/{MaxRetries}).",
                portLabel, tx.Length, attempt, maxRetries);

            // Записываем данные (синхронно)
            _port.Write(tx, 0, tx.Length);
            _port.BaseStream.Flush();

            if (writeToReadDelayMs > 0)
            {
                Thread.Sleep(writeToReadDelayMs);
            }

            // Читаем ответ (синхронно)
            var buf = new byte[expectedRxLength];
            var total = 0;

            _logger.Debug("Serial {Port}: reading {ExpectedLength} bytes with timeout {ReadTimeoutMs}ms",
                portLabel, expectedRxLength, readTimeoutMs);

            var stopwatch = Stopwatch.StartNew();

            while (total < expectedRxLength)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var bytesToRead = expectedRxLength - total;
                    var read = _port.Read(buf, total, bytesToRead);

                    if (read > 0)
                    {
                        total += read;
                        _logger.Debug("Serial {Port}: read {ReadBytes}/{ExpectedLength} bytes",
                            portLabel, total, expectedRxLength);
                    }
                }
                catch (TimeoutException)
                {
                    var elapsed = stopwatch.ElapsedMilliseconds;
                    _logger.Warning("Serial {Port}: read timeout after {ElapsedMs}ms (got {Total}/{Expected} bytes)",
                        portLabel, elapsed, total, expectedRxLength);

                    throw new TimeoutException($"Таймаут чтения: получено {total}/{expectedRxLength} байт за {elapsed}мс");
                }

                // Если мы получили хоть какие-то данные, но не все, продолжаем пытаться
                // Но проверяем общий таймаут
                if (stopwatch.ElapsedMilliseconds > readTimeoutMs)
                {
                    throw new TimeoutException($"Общий таймаут чтения: {readTimeoutMs}мс");
                }
            }

            // Очищаем буфер, если есть лишние данные
            if (_port.BytesToRead > 0)
            {
                var extraBytes = _port.BytesToRead;
                _port.DiscardInBuffer();
                _logger.Warning("Serial {Port}: discarded {ExtraBytes} extra bytes from buffer",
                    portLabel, extraBytes);
            }

            _logger.Debug("Serial {Port}: successfully read {Total} bytes in {ElapsedMs}ms",
                portLabel, total, stopwatch.ElapsedMilliseconds);

            return buf;
        }

        private static bool IsRecoverableException(Exception ex) =>
            ex is TimeoutException || ex is IOException || ex is InvalidOperationException;

        #endregion
    }
}

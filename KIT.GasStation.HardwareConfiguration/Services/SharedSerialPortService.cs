using Serilog;
using System.Diagnostics;
using System.IO.Compression;
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

#if DEBUG
            _logger.Debug("Serial {Port}: waiting for I/O lock.", portLabel);
#endif

            await _io.WaitAsync(ct);

            try
            {
#if DEBUG
                _logger.Debug("Serial {Port}: acquired I/O lock.", portLabel);
#endif

                if (PortRequiresReopen())
                {
#if DEBUG
                    _logger.Warning("Serial {Port}: port requires reopen before I/O.", portLabel);
#endif
                    await RecoverPortAsync(ct);
                }

                // Используем Task.Run для вызова синхронного метода в фоновом потоке
                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    try
                    {
#if DEBUG
                        _logger.Debug("Serial {Port}: starting operation (attempt {Attempt}/{MaxRetries})", portLabel, attempt, maxRetries);
#endif

                        // Выполняем синхронную операцию в фоновом потоке
                        result = await Task.Run(() =>
                            WriteReadOnce(tx, expectedRxLength, writeToReadDelayMs,
                                readTimeoutMs, attempt, maxRetries, portLabel, ct), ct);

#if DEBUG
                        _logger.Debug("Serial {Port}: operation successful (attempt {Attempt})", portLabel, attempt);
#endif
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

        public async Task<byte[]> WriteReadTwotpFrameAsync(
            byte[] tx,
            int writeToReadDelayMs = 20,
            int readTimeoutMs = 3000,
            int maxRetries = 3,
            bool discardInputBeforeTx = true,
            bool removeEcho = true,
            CancellationToken ct = default)
        {
            var portLabel = _key.PortName ?? "<unset>";
            byte[]? result = null;

            await _io.WaitAsync(ct);
            try
            {
                if (PortRequiresReopen())
                    await RecoverPortAsync(ct);

                for (int attempt = 1; attempt <= maxRetries; attempt++)
                {
                    ct.ThrowIfCancellationRequested();

                    try
                    {
                        result = await Task.Run(() =>
                        {
                            return WriteReadOnce_TwotpFrame(
                                tx,
                                writeToReadDelayMs,
                                readTimeoutMs,
                                discardInputBeforeTx,
                                portLabel,
                                ct);
                        }, ct);

                        if (removeEcho)
                            result = RemoveEchoIfPresent(result, tx);

                        return result;
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (TimeoutException) when (attempt < maxRetries)
                    {
                        _logger.Warning("Serial {Port}: TWOTP frame timeout (attempt {Attempt}/{MaxRetries})",
                            portLabel, attempt, maxRetries);

                        await Task.Delay(100 * attempt, ct);
                    }
                    catch (Exception ex) when (attempt < maxRetries && IsRecoverableException(ex))
                    {
                        _logger.Warning(ex, "Serial {Port}: TWOTP recoverable error (attempt {Attempt}/{MaxRetries})",
                            portLabel, attempt, maxRetries);

                        await Task.Delay(100 * attempt, ct);
                    }
                }

                throw new IOException($"Не удалось получить TWOTP кадр после {maxRetries} попыток на {portLabel}");
            }
            finally
            {
                _io.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync();
            _io.Dispose();
        }

        #endregion

        #region Private Helpers

        private byte[] WriteReadOnce_TwotpFrame(
            byte[] tx,
            int writeToReadDelayMs,
            int readTimeoutMs,
            bool discardInputBeforeTx,
            string portLabel,
            CancellationToken ct)
        {
            // короткий ReadTimeout, чтобы "пульсировать" и проверять общий таймер
            _port.ReadTimeout = 120;
            _port.WriteTimeout = 1000;

            if (discardInputBeforeTx)
            {
                // ВАЖНО: очищаем вход ДО команды (чтобы не примешались хвосты старого)
                // Но не очищаем ПОСЛЕ чтения, потому что это может быть начало следующего кадра.
                _port.DiscardInBuffer();
            }

            _port.Write(tx, 0, tx.Length);
            _port.BaseStream.Flush();

            if (writeToReadDelayMs > 0)
                Thread.Sleep(writeToReadDelayMs);

            var sw = Stopwatch.StartNew();
            var buffer = new List<byte>(512);
            var tmp = new byte[256];

            while (sw.ElapsedMilliseconds <= readTimeoutMs)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    int read = _port.Read(tmp, 0, tmp.Length);
                    if (read > 0)
                    {
                        for (int i = 0; i < read; i++)
                            buffer.Add(tmp[i]);

                        if (TryExtractTwotpFrame(buffer, out var frame))
                            return frame;
                    }
                }
                catch (TimeoutException)
                {
                    // нормально: просто пока не пришло
                }
            }

            // для диагностики можно залогировать buffer.Count
            throw new TimeoutException($"Serial {portLabel}: TWOTP frame timeout. Collected {buffer.Count} bytes.");
        }

        private static bool TryExtractTwotpFrame(List<byte> buffer, out byte[] frame)
        {
            frame = Array.Empty<byte>();

            // 1) найти STX = FF
            int stx = buffer.IndexOf(0xFF);
            if (stx < 0)
            {
                buffer.Clear();
                return false;
            }

            // выкинуть всё до FF
            if (stx > 0)
                buffer.RemoveRange(0, stx);

            // 2) найти конец кадра: FB <LRC> F0
            for (int i = 0; i <= buffer.Count - 3; i++)
            {
                if (buffer[i] == 0xFB && buffer[i + 2] == 0xF0)
                {
                    int len = i + 3; // FB + LRC + F0
                    frame = buffer.GetRange(0, len).ToArray();
                    buffer.RemoveRange(0, len);
                    return true;
                }
            }

            return false;
        }

        private static byte[] RemoveEchoIfPresent(byte[] rx, byte[] tx)
        {
            if (rx.Length < tx.Length) return rx;

            for (int i = 0; i < tx.Length; i++)
                if (rx[i] != tx[i])
                    return rx;

            // rx начинается с tx
            return rx.Skip(tx.Length).ToArray();
        }

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
            // Создаём файловый логгер теперь, когда имя порта известно
            CreatePortLogger(key.PortName);

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

        /// <summary>
        /// Устанавливает bootstrap-логгер до того как известно имя порта.
        /// Использует глобальный Serilog.Log.Logger приложения.
        /// </summary>
        private void CreateLogger()
        {
            _logger = Log.Logger;
        }

        /// <summary>
        /// Создаёт файловый логгер, уникальный для данного порта.
        /// Файл: logs/ports/{portName}_{date}.log, rolling по дням.
        /// Перед созданием архивирует (GZip) логи предыдущих дней.
        /// </summary>
        private void CreatePortLogger(string portName)
        {
            var logRoot = Path.Combine(AppContext.BaseDirectory, "logs", "ports");
            Directory.CreateDirectory(logRoot);

            ArchivePreviousDayLogs(logRoot, portName);

            string safeName = Sanitize(portName);
            // Serilog добавит дату автоматически через rollingInterval
            string pathTemplate = Path.Combine(logRoot, $"{safeName}-.log");

            _logger = new LoggerConfiguration()
                .MinimumLevel.Error()
                .WriteTo.File(
                    path: pathTemplate,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    shared: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
                )
                .CreateLogger();
        }

        /// <summary>
        /// Архивирует (GZip) файлы логов данного порта за прошлые дни.
        /// Исходный .log удаляется после успешного сжатия.
        /// </summary>
        private static void ArchivePreviousDayLogs(string logRoot, string portName)
        {
            try
            {
                string safeName = Sanitize(portName);
                string today = DateTime.Today.ToString("yyyyMMdd");

                foreach (var logFile in Directory.GetFiles(logRoot, $"{safeName}-*.log"))
                {
                    // Пропускаем файл текущего дня
                    if (Path.GetFileName(logFile).Contains(today))
                        continue;

                    string gzPath = logFile + ".gz";
                    if (File.Exists(gzPath))
                    {
                        // Архив уже есть — просто удаляем исходник если он завис
                        TryDelete(logFile);
                        continue;
                    }

                    using (var input = File.OpenRead(logFile))
                    using (var output = File.Create(gzPath))
                    using (var gz = new GZipStream(output, CompressionLevel.Optimal))
                        input.CopyTo(gz);

                    TryDelete(logFile);
                }
            }
            catch
            {
                // Архивирование не критично — не прерываем запуск
            }
        }

        private static void TryDelete(string path)
        {
            try { File.Delete(path); } catch { /* best effort */ }
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
            // Pulse-таймаут: каждый Read() ждёт не более 120ms, общий таймер — readTimeoutMs.
            // Это не блокирует поток надолго и даёт CancellationToken возможность прерваться.
            _port.ReadTimeout = 60;
            _port.WriteTimeout = 1000;

#if DEBUG
            _logger.Debug("Serial {Port}: TX {TxLen} bytes (attempt {Attempt}/{MaxRetries}): {Hex}",
                portLabel, tx.Length, attempt, maxRetries, BitConverter.ToString(tx));
#endif

            _port.Write(tx, 0, tx.Length);

            if (writeToReadDelayMs > 0)
                Thread.Sleep(writeToReadDelayMs);

            var buf = new byte[expectedRxLength];
            int total = 0;
            var sw = Stopwatch.StartNew();

            while (total < expectedRxLength && sw.ElapsedMilliseconds <= readTimeoutMs)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    int read = _port.Read(buf, total, expectedRxLength - total);
                    if (read > 0)
                        total += read;
                }
                catch (TimeoutException)
                {
                    // Pulse-таймаут истёк — данные ещё не пришли, проверяем общий таймер
                }
            }

            if (total < expectedRxLength)
            {
                string rxHex = total > 0 ? BitConverter.ToString(buf, 0, total) : "(пусто)";
                _logger.Error("Serial {Port}: timeout after {ElapsedMs}ms, got {Total}/{Expected} bytes (attempt {Attempt}/{MaxRetries}). TX: {TxHex} | RX: {RxHex}",
                    portLabel, sw.ElapsedMilliseconds, total, expectedRxLength, attempt, maxRetries,
                    BitConverter.ToString(tx), rxHex);
            }

            if (_port.BytesToRead > 0)
                _port.DiscardInBuffer();

#if DEBUG
            _logger.Debug("Serial {Port}: RX {Total} bytes in {ElapsedMs}ms: {Hex}",
                portLabel, total, sw.ElapsedMilliseconds, BitConverter.ToString(buf));
#endif

            return buf;
        }

        private static bool IsRecoverableException(Exception ex) =>
            ex is TimeoutException || ex is IOException || ex is InvalidOperationException;

        #endregion
    }
}

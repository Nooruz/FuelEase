using Serilog;
using Serilog.Core;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace KIT.GasStation.Lanfeng.Utilities
{
    /// <summary>
    /// Per-instance логгер для ТРК Lanfeng.
    /// Создаёт два файла:
    ///   - Lanfeng_{COM}_{Address}_{dd.MM.yyyy}.log   — Tx/Rx фреймы
    ///   - Lanfeng_{COM}_{Address}_{dd.MM.yyyy}_error.log — ошибки
    /// Логи предыдущих дней автоматически архивируются (GZip).
    /// </summary>
    public sealed class LanfengDeviceLogger : IDisposable
    {
        private readonly Logger _dataLogger;
        private readonly Logger _errorLogger;
        private readonly string _prefix;
        private bool _disposed;

        private const string OutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

        public LanfengDeviceLogger(string comPort, int address)
        {
            var safeCom = Sanitize(comPort);
            _prefix = $"Lanfeng_{safeCom}_{address}";

            var logRoot = Path.Combine(AppContext.BaseDirectory, "logs", "lanfeng");
            Directory.CreateDirectory(logRoot);

            // Архивируем логи предыдущих дней
            ArchivePreviousDayLogs(logRoot, _prefix);

            // Data-логгер (Tx/Rx)
            _dataLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: Path.Combine(logRoot, $"{_prefix}-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 50_000_000, // 50 MB
                    rollOnFileSizeLimit: true,
                    shared: true,
                    outputTemplate: OutputTemplate)
                .CreateLogger();

            // Error-логгер
            _errorLogger = new LoggerConfiguration()
                .MinimumLevel.Warning()
                .WriteTo.File(
                    path: Path.Combine(logRoot, $"{_prefix}_error-.log"),
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 14,
                    fileSizeLimitBytes: 20_000_000, // 20 MB
                    rollOnFileSizeLimit: true,
                    shared: true,
                    outputTemplate: OutputTemplate)
                .CreateLogger();

            _dataLogger.Information("=== Логгер инициализирован: {Prefix} ===", _prefix);
        }

        /// <summary>Лог отправленного фрейма.</summary>
        public void LogTx(byte[] frame)
        {
            _dataLogger.Information("[Tx] {Frame}", BitConverter.ToString(frame));
        }

        /// <summary>Лог полученного фрейма.</summary>
        public void LogRx(byte[] frame)
        {
            _dataLogger.Information("[Rx] {Frame}", BitConverter.ToString(frame));
        }

        /// <summary>Информационное сообщение (только в data-лог).</summary>
        public void Info(string messageTemplate, params object[] args)
        {
            _dataLogger.Information(messageTemplate, args);
        }

        /// <summary>Отладочное сообщение (только в data-лог).</summary>
        public void Debug(string messageTemplate, params object[] args)
        {
            _dataLogger.Debug(messageTemplate, args);
        }

        /// <summary>Предупреждение (в оба лога).</summary>
        public void Warning(string messageTemplate, params object[] args)
        {
            _dataLogger.Warning(messageTemplate, args);
            _errorLogger.Warning(messageTemplate, args);
        }

        /// <summary>Предупреждение с исключением (в оба лога).</summary>
        public void Warning(Exception? ex, string messageTemplate, params object[] args)
        {
            _dataLogger.Warning(ex, messageTemplate, args);
            _errorLogger.Warning(ex, messageTemplate, args);
        }

        /// <summary>Ошибка (в оба лога).</summary>
        public void Error(Exception? ex, string messageTemplate, params object[] args)
        {
            _dataLogger.Error(ex, messageTemplate, args);
            _errorLogger.Error(ex, messageTemplate, args);
        }

        #region Archive

        /// <summary>
        /// Архивирует (GZip) файлы логов за прошлые дни.
        /// Исходный .log удаляется после успешного сжатия.
        /// </summary>
        private static void ArchivePreviousDayLogs(string logRoot, string prefix)
        {
            try
            {
                string today = DateTime.Today.ToString("yyyyMMdd");

                foreach (var logFile in Directory.GetFiles(logRoot, $"{prefix}*.log"))
                {
                    var fileName = Path.GetFileName(logFile);

                    // Пропускаем файл текущего дня
                    if (fileName.Contains(today))
                        continue;

                    string gzPath = logFile + ".gz";
                    if (File.Exists(gzPath))
                    {
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

        private static string Sanitize(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "UNNAMED";
            s = s.Trim();
            s = Regex.Replace(s, @"[^\w\-\.\(\) ]+", "_");
            return s.Length > 80 ? s[..80] : s;
        }

        #endregion

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _dataLogger.Dispose();
            _errorLogger.Dispose();
        }
    }
}

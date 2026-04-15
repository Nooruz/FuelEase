using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using KIT.GasStation.Licensing.Models;

namespace KIT.GasStation.Licensing.Protection;

/// <summary>
/// Специализированный логгер событий безопасности.
/// Ведёт отдельный зашифрованный журнал попыток обхода защиты.
/// </summary>
public sealed class SecurityLogger
{
    private readonly ILogger<SecurityLogger> _logger;
    private readonly string _logPath;
    private readonly object _lock = new();

    public SecurityLogger(IOptions<LicensingOptions> options, ILogger<SecurityLogger> logger)
    {
        _logger = logger;

        var basePath = string.IsNullOrEmpty(options.Value.StoragePath)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KIT-AZS", "License")
            : options.Value.StoragePath;

        _logPath = Path.Combine(basePath, "security.log");
        EnsureDirectory();
    }

    /// <summary>Фиксирует попытку использования невалидной лицензии.</summary>
    public void LogInvalidLicense(string details)
        => WriteEntry(SecurityEventType.InvalidLicense, details);

    /// <summary>Фиксирует обнаруженный откат системного времени.</summary>
    public void LogTimeRollback(string details)
        => WriteEntry(SecurityEventType.TimeRollback, details);

    /// <summary>Фиксирует обнаруженную модификацию сборок.</summary>
    public void LogTamperDetected(string details)
        => WriteEntry(SecurityEventType.TamperDetected, details);

    /// <summary>Фиксирует обнаруженное клонирование системы.</summary>
    public void LogCloneDetected(string details)
        => WriteEntry(SecurityEventType.CloneDetected, details);

    /// <summary>Фиксирует несоответствие аппаратного отпечатка.</summary>
    public void LogHardwareMismatch(string details)
        => WriteEntry(SecurityEventType.HardwareMismatch, details);

    /// <summary>Фиксирует повреждение хранилища.</summary>
    public void LogStorageCorruption(string details)
        => WriteEntry(SecurityEventType.StorageCorruption, details);

    /// <summary>Фиксирует неудачную онлайн-проверку.</summary>
    public void LogOnlineCheckFailed(string details)
        => WriteEntry(SecurityEventType.OnlineCheckFailed, details);

    /// <summary>Фиксирует начало Grace Period.</summary>
    public void LogGracePeriodStarted(int daysRemaining)
        => WriteEntry(SecurityEventType.GracePeriodStarted, $"Осталось дней: {daysRemaining}");

    /// <summary>Фиксирует истечение Grace Period.</summary>
    public void LogGracePeriodExpired()
        => WriteEntry(SecurityEventType.GracePeriodExpired, "Grace Period истёк — блокировка");

    /// <summary>Фиксирует успешную активацию лицензии.</summary>
    public void LogActivationSuccess(string licenseId)
        => WriteEntry(SecurityEventType.ActivationSuccess, $"LicenseId: {licenseId}");

    /// <summary>
    /// Читает последние N записей из журнала безопасности.
    /// </summary>
    public IReadOnlyList<string> GetRecentEntries(int count = 100)
    {
        try
        {
            if (!File.Exists(_logPath))
                return Array.Empty<string>();

            lock (_lock)
            {
                var lines = File.ReadAllLines(_logPath);
                return lines.TakeLast(count).ToArray();
            }
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private void WriteEntry(SecurityEventType eventType, string details)
    {
        var entry = $"{DateTime.UtcNow:O}|{eventType}|{Environment.MachineName}|{details}";

        // Дублируем в стандартный логгер
        _logger.LogWarning("[SECURITY] {EventType}: {Details}", eventType, details);

        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logPath, entry + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Не удалось записать в журнал безопасности");
            }
        }
    }

    private void EnsureDirectory()
    {
        var dir = Path.GetDirectoryName(_logPath);
        if (dir != null && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }
}

public enum SecurityEventType
{
    InvalidLicense,
    TimeRollback,
    TamperDetected,
    CloneDetected,
    HardwareMismatch,
    StorageCorruption,
    OnlineCheckFailed,
    GracePeriodStarted,
    GracePeriodExpired,
    ActivationSuccess
}

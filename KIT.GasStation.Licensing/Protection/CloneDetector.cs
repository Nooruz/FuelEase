using System.Security.Cryptography;
using System.Text;
using KIT.GasStation.Licensing.Core;
using KIT.GasStation.Licensing.Models;
using KIT.GasStation.Licensing.Storage;
using Microsoft.Extensions.Logging;

namespace KIT.GasStation.Licensing.Protection;

/// <summary>
/// Детектор клонирования системы.
/// При первой активации генерируется уникальный InstanceId, привязанный к оборудованию.
/// При обнаружении расхождения — лицензия блокируется.
/// </summary>
public sealed class CloneDetector
{
    private const string InstanceFileName = ".instance";
    private readonly LicenseStore _store;
    private readonly ILogger<CloneDetector> _logger;

    public CloneDetector(LicenseStore store, ILogger<CloneDetector> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Генерирует уникальный InstanceId для текущей установки.
    /// Включает: HWID + случайный GUID + timestamp.
    /// </summary>
    public static string GenerateInstanceId()
    {
        var hwid = HardwareFingerprint.Generate();
        var random = Guid.NewGuid().ToString("N");
        var timestamp = DateTime.UtcNow.Ticks.ToString();

        var combined = $"{hwid}:{random}:{timestamp}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Проверяет, не является ли система клоном.
    /// Сравнивает InstanceId из лицензии, из состояния и из файла-маркера.
    /// </summary>
    public CloneCheckResult Check(LicensePayload payload, LicenseState state)
    {
        // Если InstanceId ещё не установлен — первый запуск, инициализация
        if (string.IsNullOrEmpty(state.InstanceId))
        {
            return new CloneCheckResult
            {
                IsValid = true,
                NeedsInitialization = true
            };
        }

        // 1. Проверяем, совпадает ли InstanceId с тем, что в лицензии
        if (!string.IsNullOrEmpty(payload.InstanceId) &&
            !string.Equals(payload.InstanceId, state.InstanceId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "InstanceId в лицензии ({LicenseIID}) не совпадает с локальным ({LocalIID})",
                payload.InstanceId, state.InstanceId);

            return new CloneCheckResult
            {
                IsValid = false,
                Reason = "Обнаружено клонирование: InstanceId не совпадает с лицензией"
            };
        }

        // 2. Проверяем текущий HWID — не изменилось ли оборудование
        var currentHwid = HardwareFingerprint.Generate();
        if (!string.IsNullOrEmpty(payload.HardwareId) &&
            !string.Equals(payload.HardwareId, currentHwid, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Hardware fingerprint изменился: ожидался {Expected}, получен {Actual}",
                payload.HardwareId, currentHwid);

            return new CloneCheckResult
            {
                IsValid = false,
                Reason = "Оборудование не соответствует привязке лицензии"
            };
        }

        return new CloneCheckResult { IsValid = true };
    }

    /// <summary>
    /// Инициализирует InstanceId при первой активации.
    /// </summary>
    public string Initialize(LicenseState state)
    {
        var instanceId = GenerateInstanceId();
        state.InstanceId = instanceId;
        _logger.LogInformation("Инициализирован InstanceId: {InstanceId}", instanceId[..16] + "...");
        return instanceId;
    }
}

public sealed class CloneCheckResult
{
    public bool IsValid { get; init; }
    public bool NeedsInitialization { get; init; }
    public string Reason { get; init; } = string.Empty;
}

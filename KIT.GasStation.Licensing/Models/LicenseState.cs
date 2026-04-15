using System.Text.Json.Serialization;

namespace KIT.GasStation.Licensing.Models;

/// <summary>
/// Локальное состояние лицензии — сохраняется в защищённом хранилище.
/// </summary>
public sealed class LicenseState
{
    /// <summary>Текущий статус.</summary>
    [JsonPropertyName("status")]
    public LicenseStatus Status { get; set; } = LicenseStatus.NotActivated;

    /// <summary>Последний успешный онлайн-чек (UTC).</summary>
    [JsonPropertyName("lastOnlineCheck")]
    public DateTime LastOnlineCheckUtc { get; set; }

    /// <summary>Последнее известное серверное время (для детекта rollback).</summary>
    [JsonPropertyName("lastServerTime")]
    public DateTime LastServerTimeUtc { get; set; }

    /// <summary>Последнее локальное время при проверке (для детекта rollback).</summary>
    [JsonPropertyName("lastLocalTime")]
    public DateTime LastLocalTimeUtc { get; set; }

    /// <summary>Монотонный счётчик тиков (Environment.TickCount64 на момент проверки).</summary>
    [JsonPropertyName("lastTick")]
    public long LastTickCount { get; set; }

    /// <summary>Уникальный ID экземпляра (генерируется при первой активации).</summary>
    [JsonPropertyName("instanceId")]
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>Токен lease от сервера (обновляется периодически).</summary>
    [JsonPropertyName("leaseToken")]
    public string LeaseToken { get; set; } = string.Empty;

    /// <summary>Истечение текущего lease (UTC).</summary>
    [JsonPropertyName("leaseExpiry")]
    public DateTime LeaseExpiryUtc { get; set; }

    /// <summary>Начало Grace Period (UTC). DateTime.MinValue = не в grace period.</summary>
    [JsonPropertyName("graceStart")]
    public DateTime GracePeriodStartUtc { get; set; } = DateTime.MinValue;

    /// <summary>Количество последовательных неудачных онлайн-проверок.</summary>
    [JsonPropertyName("failedChecks")]
    public int ConsecutiveFailedOnlineChecks { get; set; }

    /// <summary>HMAC-подпись состояния (защита от подмены).</summary>
    [JsonPropertyName("hmac")]
    public string IntegrityHmac { get; set; } = string.Empty;
}

public enum LicenseStatus
{
    NotActivated = 0,
    Active = 1,
    GracePeriod = 2,
    Expired = 3,
    Revoked = 4,
    HardwareMismatch = 5,
    Tampered = 6,
    CloneDetected = 7
}

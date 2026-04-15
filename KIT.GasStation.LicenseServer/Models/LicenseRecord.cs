namespace KIT.GasStation.LicenseServer.Models;

/// <summary>Запись о лицензии в БД.</summary>
public class LicenseRecord
{
    public int Id { get; set; }

    /// <summary>Уникальный ключ лицензии (выдаётся клиенту для активации).</summary>
    public string LicenseKey { get; set; } = string.Empty;

    /// <summary>ID лицензии (внутренний, фигурирует в подписанном payload).</summary>
    public string LicenseId { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Product { get; set; } = "KIT.GasStation";
    public string Tier { get; set; } = "Standard";
    public string Features { get; set; } = string.Empty;
    public int MaxSeats { get; set; } = 1;

    public DateTime IssuedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }

    public bool IsRevoked { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string RevokeReason { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Запись об активации лицензии на конкретном устройстве.</summary>
public class ActivationRecord
{
    public int Id { get; set; }
    public string LicenseId { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;

    /// <summary>Текущий lease-токен для данной активации.</summary>
    public string LeaseToken { get; set; } = string.Empty;
    public DateTime LeaseExpiryUtc { get; set; }

    public DateTime ActivatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeatUtc { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsCloneSuspect { get; set; }
}

/// <summary>Лог heartbeat-запросов.</summary>
public class HeartbeatLog
{
    public long Id { get; set; }
    public string LicenseId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public DateTime ClientTimeUtc { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string IpAddress { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Details { get; set; } = string.Empty;
}

/// <summary>Событие безопасности (серверная сторона).</summary>
public class SecurityEvent
{
    public long Id { get; set; }
    public string LicenseId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

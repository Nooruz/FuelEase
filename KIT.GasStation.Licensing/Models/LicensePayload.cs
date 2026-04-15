using System.Text.Json.Serialization;

namespace KIT.GasStation.Licensing.Models;

/// <summary>
/// Данные лицензии, подписываемые RSA-ключом на сервере.
/// </summary>
public sealed class LicensePayload
{
    /// <summary>Уникальный идентификатор лицензии.</summary>
    [JsonPropertyName("id")]
    public string LicenseId { get; set; } = string.Empty;

    /// <summary>Идентификатор клиента (организации).</summary>
    [JsonPropertyName("cid")]
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>Название продукта.</summary>
    [JsonPropertyName("prod")]
    public string Product { get; set; } = "KIT.GasStation";

    /// <summary>Тип лицензии: Standard, Professional, Enterprise.</summary>
    [JsonPropertyName("tier")]
    public string Tier { get; set; } = "Standard";

    /// <summary>Дата выдачи лицензии (UTC).</summary>
    [JsonPropertyName("iss")]
    public DateTime IssuedAtUtc { get; set; }

    /// <summary>Дата истечения лицензии (UTC).</summary>
    [JsonPropertyName("exp")]
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>SHA-256 хеш аппаратного отпечатка, к которому привязана лицензия.</summary>
    [JsonPropertyName("hwid")]
    public string HardwareId { get; set; } = string.Empty;

    /// <summary>Уникальный идентификатор экземпляра установки (для детекта клонирования).</summary>
    [JsonPropertyName("iid")]
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>Максимальное количество одновременных устройств (для enterprise).</summary>
    [JsonPropertyName("seats")]
    public int MaxSeats { get; set; } = 1;

    /// <summary>Включённые модули/фичи (через запятую).</summary>
    [JsonPropertyName("feat")]
    public string Features { get; set; } = string.Empty;
}

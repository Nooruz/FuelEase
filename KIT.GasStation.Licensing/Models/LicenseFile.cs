using System.Text.Json.Serialization;

namespace KIT.GasStation.Licensing.Models;

/// <summary>
/// Файл лицензии = payload (JSON) + цифровая подпись (Base64).
/// </summary>
public sealed class LicenseFile
{
    /// <summary>Подписанный payload в Base64.</summary>
    [JsonPropertyName("payload")]
    public string PayloadBase64 { get; set; } = string.Empty;

    /// <summary>RSA-подпись payload в Base64.</summary>
    [JsonPropertyName("sig")]
    public string SignatureBase64 { get; set; } = string.Empty;

    /// <summary>Версия формата лицензии.</summary>
    [JsonPropertyName("ver")]
    public int Version { get; set; } = 1;
}

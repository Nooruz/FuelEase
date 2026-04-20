namespace KIT.GasStation.Licensing.Models;

/// <summary>
/// Конфигурация модуля лицензирования.
/// </summary>
public sealed class LicensingOptions
{
    public const string SectionName = "Licensing";

    /// <summary>URL сервера лицензирования.</summary>
    public string ServerUrl { get; set; } = "https://license.kit-azs.ru";

    /// <summary>Публичный RSA-ключ (Base64, X.509 SubjectPublicKeyInfo).</summary>
    public string PublicKey { get; set; } = string.Empty;

    /// <summary>Длительность Grace Period в днях.</summary>
    public int GracePeriodDays { get; set; } = 7;

    /// <summary>Интервал онлайн-проверки в минутах.</summary>
    public int OnlineCheckIntervalMinutes { get; set; } = 60;

    /// <summary>Максимум неудачных онлайн-проверок до перехода в Grace.</summary>
    public int MaxFailedOnlineChecks { get; set; } = 3;

    /// <summary>Папка для хранения файлов лицензии и состояния.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Название продукта для проверки совпадения с лицензией.</summary>
    public string ProductName { get; set; } = "KIT.GasStation";

    /// <summary>Список защищаемых сборок (для anti-tamper проверки).</summary>
    public List<string> ProtectedAssemblies { get; set; } = new();

    /// <summary>
    /// Ключ лицензии для автоматической активации при первом запуске.
    /// После успешной активации сервис продолжит работу используя сохранённый файл лицензии.
    /// Можно оставить пустым, если лицензия уже активирована.
    /// </summary>
    public string LicenseKey { get; set; } = string.Empty;

    /// <summary>
    /// Отключить проверку целостности сборок (AntiTamper).
    /// Устанавливать true ТОЛЬКО в appsettings.Development.json — в Production всегда false.
    /// </summary>
    public bool DisableAntiTamper { get; set; } = false;

    /// <summary>
    /// Полностью отключить систему лицензирования: пропустить активацию,
    /// проверку подписи, онлайн-heartbeat и все остальные проверки.
    /// Лицензия считается действительной автоматически.
    /// ТОЛЬКО для разработки — в Production всегда false.
    /// </summary>
    public bool DisableLicensing { get; set; } = false;
}

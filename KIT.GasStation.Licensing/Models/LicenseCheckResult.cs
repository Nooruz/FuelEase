namespace KIT.GasStation.Licensing.Models;

/// <summary>
/// Результат проверки лицензии.
/// </summary>
public sealed class LicenseCheckResult
{
    public bool IsValid { get; init; }
    public LicenseStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public LicensePayload? Payload { get; init; }
    public TimeSpan? GraceRemaining { get; init; }

    public static LicenseCheckResult Valid(LicensePayload payload) => new()
    {
        IsValid = true,
        Status = LicenseStatus.Active,
        Message = "Лицензия активна",
        Payload = payload
    };

    public static LicenseCheckResult Grace(LicensePayload payload, TimeSpan remaining) => new()
    {
        IsValid = true,
        Status = LicenseStatus.GracePeriod,
        Message = $"Grace period: осталось {remaining.Days}д {remaining.Hours}ч",
        Payload = payload,
        GraceRemaining = remaining
    };

    public static LicenseCheckResult Invalid(LicenseStatus status, string message) => new()
    {
        IsValid = false,
        Status = status,
        Message = message
    };
}

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using KIT.GasStation.Licensing.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KIT.GasStation.Licensing.Core;

/// <summary>
/// Проверка цифровой подписи, срока действия и привязки к оборудованию.
/// </summary>
public sealed class LicenseValidator
{
    private readonly LicensingOptions _options;
    private readonly ILogger<LicenseValidator> _logger;
    private readonly RSA _rsa;

    public LicenseValidator(IOptions<LicensingOptions> options, ILogger<LicenseValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
        _rsa = RSA.Create();

        if (!string.IsNullOrEmpty(_options.PublicKey))
        {
            _rsa.ImportSubjectPublicKeyInfo(Convert.FromBase64String(_options.PublicKey), out _);
        }
    }

    /// <summary>
    /// Полная валидация файла лицензии: подпись → срок → привязка к железу.
    /// </summary>
    public LicenseCheckResult Validate(LicenseFile licenseFile, string currentHardwareId)
    {
        // 1. Проверка подписи
        var payload = VerifySignature(licenseFile);
        if (payload == null)
        {
            return LicenseCheckResult.Invalid(LicenseStatus.Tampered,
                "Цифровая подпись лицензии недействительна");
        }

        // 2. Проверка продукта
        if (!string.Equals(payload.Product, _options.ProductName, StringComparison.OrdinalIgnoreCase))
        {
            return LicenseCheckResult.Invalid(LicenseStatus.Tampered,
                $"Лицензия выдана для другого продукта: {payload.Product}");
        }

        // 3. Проверка срока действия
        var now = DateTime.UtcNow;
        if (now < payload.IssuedAtUtc)
        {
            return LicenseCheckResult.Invalid(LicenseStatus.Tampered,
                "Дата выдачи лицензии в будущем — возможна подмена времени");
        }

        if (now > payload.ExpiresAtUtc)
        {
            return LicenseCheckResult.Invalid(LicenseStatus.Expired,
                $"Лицензия истекла {payload.ExpiresAtUtc:yyyy-MM-dd}");
        }

        // 4. Проверка привязки к оборудованию
        if (!string.IsNullOrEmpty(payload.HardwareId) &&
            !string.Equals(payload.HardwareId, currentHardwareId, StringComparison.OrdinalIgnoreCase))
        {
            return LicenseCheckResult.Invalid(LicenseStatus.HardwareMismatch,
                "Лицензия привязана к другому оборудованию");
        }

        return LicenseCheckResult.Valid(payload);
    }

    /// <summary>
    /// Проверяет только подпись и десериализует payload.
    /// </summary>
    public LicensePayload? VerifySignature(LicenseFile licenseFile)
    {
        try
        {
            var payloadBytes = Convert.FromBase64String(licenseFile.PayloadBase64);
            var signatureBytes = Convert.FromBase64String(licenseFile.SignatureBase64);

            var isValid = _rsa.VerifyData(
                payloadBytes,
                signatureBytes,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            if (!isValid)
            {
                _logger.LogWarning("Подпись лицензии не прошла проверку RSA");
                return null;
            }

            var json = Encoding.UTF8.GetString(payloadBytes);
            return JsonSerializer.Deserialize<LicensePayload>(json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при проверке подписи лицензии");
            return null;
        }
    }

    /// <summary>
    /// Проверяет, не был ли лицензионный файл изменён (быстрая проверка хеша).
    /// </summary>
    public static string ComputeFileHash(LicenseFile licenseFile)
    {
        var data = $"{licenseFile.PayloadBase64}:{licenseFile.SignatureBase64}:{licenseFile.Version}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

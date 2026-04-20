using KIT.GasStation.LicenseServer.Data;
using KIT.GasStation.LicenseServer.Models;
using KIT.GasStation.Licensing.Client;
using KIT.GasStation.Licensing.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace KIT.GasStation.LicenseServer.Services;

/// <summary>
/// Основной сервис управления лицензиями на сервере.
/// Генерация ключей, подпись, активация, heartbeat, детект клонирования.
/// </summary>
public sealed class LicenseService
{
    private readonly LicenseDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<LicenseService> _logger;
    private readonly RSA _rsa;

    /// <summary>Длительность lease в часах.</summary>
    private const int LeaseHours = 24;

    public LicenseService(LicenseDbContext db, IConfiguration config, ILogger<LicenseService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _rsa = RSA.Create();

        // Загружаем приватный ключ
        var privateKey = _config["Licensing:PrivateKey"];
        if (!string.IsNullOrEmpty(privateKey))
        {
            _rsa.ImportRSAPrivateKey(Convert.FromBase64String(privateKey), out _);
        }
    }

    #region Генерация лицензий

    /// <summary>
    /// Создаёт новую лицензию в БД и возвращает ключ активации.
    /// </summary>
    public async Task<LicenseRecord> CreateLicenseAsync(CreateLicenseRequest request)
    {
        var licenseKey = GenerateLicenseKey();
        var licenseId = Guid.NewGuid().ToString("N");

        var record = new LicenseRecord
        {
            LicenseKey = licenseKey,
            LicenseId = licenseId,
            CustomerId = request.CustomerId,
            CustomerName = request.CustomerName,
            Product = request.Product ?? "KIT.GasStation",
            Tier = request.Tier ?? "Standard",
            Features = request.Features ?? string.Empty,
            MaxSeats = request.MaxSeats > 0 ? request.MaxSeats : 1,
            IssuedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddYears(1)
        };

        _db.Licenses.Add(record);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Создана лицензия {LicenseId} для клиента {Customer}",
            licenseId, request.CustomerName);

        return record;
    }

    #endregion

    #region Активация

    /// <summary>
    /// Активирует лицензию на устройстве: проверяет ключ, создаёт подписанный файл.
    /// </summary>
    public async Task<ActivationResponse> ActivateAsync(ActivationRequest request, string ipAddress)
    {
        var license = await _db.Licenses
            .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey);

        if (license == null)
        {
            return new ActivationResponse
            {
                Success = false,
                Message = "Недействительный ключ лицензии"
            };
        }

        if (license.IsRevoked)
        {
            await LogSecurityEvent(license.LicenseId, "ActivationAttemptRevoked",
                $"Попытка активации отозванной лицензии с IP {ipAddress}", ipAddress);

            return new ActivationResponse
            {
                Success = false,
                Message = "Лицензия отозвана"
            };
        }

        if (DateTime.UtcNow > license.ExpiresAtUtc)
        {
            return new ActivationResponse
            {
                Success = false,
                Message = "Срок действия лицензии истёк"
            };
        }

        // Проверяем количество активных активаций
        var activeActivations = await _db.Activations
            .Where(a => a.LicenseId == license.LicenseId && a.IsActive)
            .ToListAsync();

        // Проверяем, не активирована ли уже на этом железе
        var existingActivation = activeActivations
            .FirstOrDefault(a => a.HardwareId == request.HardwareId);

        if (existingActivation != null)
        {
            // Повторная активация на том же железе — обновляем InstanceId и MachineName
            existingActivation.InstanceId = request.InstanceId;
            existingActivation.MachineName = request.MachineName;
            existingActivation.LeaseToken = GenerateLeaseToken();
            existingActivation.LeaseExpiryUtc = DateTime.UtcNow.AddHours(LeaseHours);
            existingActivation.LastHeartbeatUtc = DateTime.UtcNow;
        }
        else
        {
            // Новая активация
            if (activeActivations.Count >= license.MaxSeats)
            {
                await LogSecurityEvent(license.LicenseId, "MaxSeatsExceeded",
                    $"Превышение лимита активаций ({license.MaxSeats}), IP: {ipAddress}", ipAddress);

                return new ActivationResponse
                {
                    Success = false,
                    Message = $"Достигнуто максимальное количество активаций ({license.MaxSeats})"
                };
            }

            existingActivation = new ActivationRecord
            {
                LicenseId = license.LicenseId,
                HardwareId = request.HardwareId,
                InstanceId = request.InstanceId,
                MachineName = request.MachineName,
                LeaseToken = GenerateLeaseToken(),
                LeaseExpiryUtc = DateTime.UtcNow.AddHours(LeaseHours),
                LastHeartbeatUtc = DateTime.UtcNow
            };
            _db.Activations.Add(existingActivation);
        }

        await _db.SaveChangesAsync();

        // Формируем подписанный файл лицензии
        var licenseFile = SignLicense(license, request.HardwareId, request.InstanceId);

        _logger.LogInformation("Лицензия {LicenseId} активирована на {Machine} (HWID: {HWID})",
            license.LicenseId, request.MachineName, request.HardwareId[..16]);

        return new ActivationResponse
        {
            Success = true,
            Message = "Лицензия активирована",
            LicenseFile = licenseFile,
            LeaseToken = existingActivation.LeaseToken,
            LeaseExpiryUtc = existingActivation.LeaseExpiryUtc,
            ServerTimeUtc = DateTime.UtcNow
        };
    }

    #endregion

    #region Heartbeat

    /// <summary>
    /// Обрабатывает heartbeat: проверяет lease, обновляет токен, детектит клонирование.
    /// </summary>
    public async Task<HeartbeatResponse> HeartbeatAsync(HeartbeatRequest request, string ipAddress)
    {
        var activation = await _db.Activations
            .FirstOrDefaultAsync(a =>
                a.LicenseId == request.LicenseId &&
                a.InstanceId == request.InstanceId &&
                a.IsActive);

        if (activation == null)
        {
            // Нет активации с таким InstanceId — возможен клон
            var otherActivation = await _db.Activations
                .FirstOrDefaultAsync(a =>
                    a.LicenseId == request.LicenseId &&
                    a.HardwareId == request.HardwareId &&
                    a.IsActive);

            if (otherActivation != null && otherActivation.InstanceId != request.InstanceId)
            {
                // Клонирование! Другой InstanceId на том же железе
                otherActivation.IsCloneSuspect = true;
                await LogSecurityEvent(request.LicenseId, "CloneDetected",
                    $"InstanceId mismatch: expected {otherActivation.InstanceId}, got {request.InstanceId}, IP: {ipAddress}",
                    ipAddress);
                await _db.SaveChangesAsync();

                return new HeartbeatResponse
                {
                    Success = false,
                    CloneDetected = true,
                    Message = "Обнаружено клонирование системы"
                };
            }

            return new HeartbeatResponse
            {
                Success = false,
                Message = "Активация не найдена"
            };
        }

        // Проверяем, не отозвана ли лицензия
        var license = await _db.Licenses
            .FirstOrDefaultAsync(l => l.LicenseId == request.LicenseId);

        if (license == null || license.IsRevoked)
        {
            return new HeartbeatResponse
            {
                Success = false,
                Revoked = true,
                Message = "Лицензия отозвана",
                ServerTimeUtc = DateTime.UtcNow
            };
        }

        // Проверяем LeaseToken — защита от replay-атак
        if (!string.IsNullOrEmpty(activation.LeaseToken) &&
            !string.IsNullOrEmpty(request.LeaseToken) &&
            !string.Equals(activation.LeaseToken, request.LeaseToken, StringComparison.Ordinal))
        {
            await LogSecurityEvent(request.LicenseId, "InvalidLeaseToken",
                $"LeaseToken mismatch from IP: {ipAddress}", ipAddress);

            return new HeartbeatResponse
            {
                Success = false,
                Message = "Недействительный lease-токен — требуется повторная активация"
            };
        }

        // Проверяем HardwareId — не сменилось ли железо
        if (activation.HardwareId != request.HardwareId)
        {
            await LogSecurityEvent(request.LicenseId, "HardwareChanged",
                $"HWID changed from {activation.HardwareId[..16]} to {request.HardwareId[..16]}, IP: {ipAddress}",
                ipAddress);

            return new HeartbeatResponse
            {
                Success = false,
                Message = "Оборудование изменилось — требуется повторная активация"
            };
        }

        // Обновляем lease
        var newLeaseToken = GenerateLeaseToken();
        activation.LeaseToken = newLeaseToken;
        activation.LeaseExpiryUtc = DateTime.UtcNow.AddHours(LeaseHours);
        activation.LastHeartbeatUtc = DateTime.UtcNow;

        // Логируем heartbeat
        _db.HeartbeatLogs.Add(new HeartbeatLog
        {
            LicenseId = request.LicenseId,
            InstanceId = request.InstanceId,
            HardwareId = request.HardwareId,
            ClientTimeUtc = request.ClientTimeUtc,
            IpAddress = ipAddress,
            Success = true
        });

        await _db.SaveChangesAsync();

        return new HeartbeatResponse
        {
            Success = true,
            NewLeaseToken = newLeaseToken,
            LeaseExpiryUtc = activation.LeaseExpiryUtc,
            ServerTimeUtc = DateTime.UtcNow
        };
    }

    #endregion

    #region Status & Revocation

    public async Task<LicenseStatusResponse?> GetStatusAsync(string licenseId)
    {
        var license = await _db.Licenses
            .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

        if (license == null)
            return null;

        return new LicenseStatusResponse
        {
            IsActive = !license.IsRevoked && DateTime.UtcNow <= license.ExpiresAtUtc,
            IsRevoked = license.IsRevoked,
            ExpiresAtUtc = license.ExpiresAtUtc,
            Message = license.IsRevoked ? $"Отозвана: {license.RevokeReason}" : "Активна"
        };
    }

    public async Task<bool> RevokeLicenseAsync(string licenseId, string reason)
    {
        var license = await _db.Licenses
            .FirstOrDefaultAsync(l => l.LicenseId == licenseId);

        if (license == null)
            return false;

        license.IsRevoked = true;
        license.RevokedAtUtc = DateTime.UtcNow;
        license.RevokeReason = reason;

        // Деактивируем все активации
        var activations = await _db.Activations
            .Where(a => a.LicenseId == licenseId)
            .ToListAsync();

        foreach (var activation in activations)
            activation.IsActive = false;

        await LogSecurityEvent(licenseId, "Revoked", reason, "SERVER");
        await _db.SaveChangesAsync();

        _logger.LogWarning("Лицензия {LicenseId} отозвана: {Reason}", licenseId, reason);
        return true;
    }

    #endregion

    #region Helpers

    private LicenseFile SignLicense(LicenseRecord license, string hardwareId, string instanceId)
    {
        var payload = new LicensePayload
        {
            LicenseId = license.LicenseId,
            CustomerId = license.CustomerId,
            Product = license.Product,
            Tier = license.Tier,
            IssuedAtUtc = license.IssuedAtUtc,
            ExpiresAtUtc = license.ExpiresAtUtc,
            HardwareId = hardwareId,
            InstanceId = instanceId,
            MaxSeats = license.MaxSeats,
            Features = license.Features
        };

        var payloadJson = JsonSerializer.Serialize(payload);
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);
        var signature = _rsa.SignData(payloadBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        return new LicenseFile
        {
            PayloadBase64 = Convert.ToBase64String(payloadBytes),
            SignatureBase64 = Convert.ToBase64String(signature),
            Version = 1
        };
    }

    private static string GenerateLicenseKey()
    {
        // Формат: XXXX-XXXX-XXXX-XXXX-XXXX
        var bytes = RandomNumberGenerator.GetBytes(20);
        var hex = Convert.ToHexString(bytes).ToUpperInvariant();
        return string.Join("-",
            Enumerable.Range(0, 5).Select(i => hex.Substring(i * 4, 4)));
    }

    private static string GenerateLeaseToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }

    private async Task LogSecurityEvent(string licenseId, string eventType, string details, string ip)
    {
        _db.SecurityEvents.Add(new SecurityEvent
        {
            LicenseId = licenseId,
            EventType = eventType,
            Details = details,
            IpAddress = ip
        });
        await _db.SaveChangesAsync();
    }

    #endregion
}

public sealed class CreateLicenseRequest
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? Product { get; set; }
    public string? Tier { get; set; }
    public string? Features { get; set; }
    public int MaxSeats { get; set; } = 1;
}

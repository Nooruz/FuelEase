using System.Net.Http.Json;
using System.Text.Json;
using KIT.GasStation.Licensing.Core;
using KIT.GasStation.Licensing.Models;
using KIT.GasStation.Licensing.Protection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KIT.GasStation.Licensing.Client;

/// <summary>
/// HTTP-клиент для взаимодействия с сервером лицензирования.
/// Поддерживает: активацию, heartbeat/lease, проверку статуса, отзыв.
/// </summary>
public sealed class OnlineLicenseClient
{
    private readonly HttpClient _httpClient;
    private readonly LicensingOptions _options;
    private readonly ILogger<OnlineLicenseClient> _logger;

    public OnlineLicenseClient(
        HttpClient httpClient,
        IOptions<LicensingOptions> options,
        ILogger<OnlineLicenseClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_options.ServerUrl.TrimEnd('/') + "/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Активация лицензии на сервере. Отправляет HWID и получает подписанную лицензию.
    /// </summary>
    public async Task<ActivationResponse?> ActivateAsync(
        string licenseKey,
        string hardwareId,
        string instanceId,
        CancellationToken ct = default)
    {
        try
        {
            var request = new ActivationRequest
            {
                LicenseKey = licenseKey,
                HardwareId = hardwareId,
                InstanceId = instanceId,
                MachineName = Environment.MachineName
            };

            var response = await _httpClient.PostAsJsonAsync("api/license/activate", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Активация отклонена: {Status} {Error}",
                    response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ActivationResponse>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при активации лицензии");
            return null;
        }
    }

    /// <summary>
    /// Heartbeat / обновление lease. Подтверждает серверу, что устройство активно.
    /// Возвращает серверное время и обновлённый lease-токен.
    /// </summary>
    public async Task<HeartbeatResponse?> HeartbeatAsync(
        string licenseId,
        string instanceId,
        string hardwareId,
        string leaseToken,
        CancellationToken ct = default)
    {
        try
        {
            var request = new HeartbeatRequest
            {
                LicenseId = licenseId,
                InstanceId = instanceId,
                HardwareId = hardwareId,
                LeaseToken = leaseToken,
                ClientTimeUtc = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync("api/license/heartbeat", request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Heartbeat отклонён: {Status} {Error}",
                    response.StatusCode, error);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<HeartbeatResponse>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Heartbeat не удался (сервер недоступен?)");
            return null;
        }
    }

    /// <summary>
    /// Проверяет статус лицензии на сервере (не отозвана ли, не заблокирована ли).
    /// </summary>
    public async Task<LicenseStatusResponse?> CheckStatusAsync(
        string licenseId,
        CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"api/license/status/{licenseId}", ct);

            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<LicenseStatusResponse>(cancellationToken: ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Проверка статуса лицензии не удалась");
            return null;
        }
    }
}

#region DTO Models

public sealed class ActivationRequest
{
    public string LicenseKey { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
}

public sealed class ActivationResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public LicenseFile? LicenseFile { get; set; }
    public string LeaseToken { get; set; } = string.Empty;
    public DateTime LeaseExpiryUtc { get; set; }
    public DateTime ServerTimeUtc { get; set; }
}

public sealed class HeartbeatRequest
{
    public string LicenseId { get; set; } = string.Empty;
    public string InstanceId { get; set; } = string.Empty;
    public string HardwareId { get; set; } = string.Empty;
    public string LeaseToken { get; set; } = string.Empty;
    public DateTime ClientTimeUtc { get; set; }
}

public sealed class HeartbeatResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string NewLeaseToken { get; set; } = string.Empty;
    public DateTime LeaseExpiryUtc { get; set; }
    public DateTime ServerTimeUtc { get; set; }
    public bool Revoked { get; set; }
    public bool CloneDetected { get; set; }
}

public sealed class LicenseStatusResponse
{
    public bool IsActive { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public string Message { get; set; } = string.Empty;
}

#endregion

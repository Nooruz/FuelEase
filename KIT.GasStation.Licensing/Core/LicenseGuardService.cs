using KIT.GasStation.Licensing.Client;
using KIT.GasStation.Licensing.Models;
using KIT.GasStation.Licensing.Protection;
using KIT.GasStation.Licensing.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KIT.GasStation.Licensing.Core;

/// <summary>
/// Фоновый сервис: периодическая проверка лицензии, heartbeat, grace period.
/// Интегрируется как IHostedService — при блокировке останавливает хост.
/// </summary>
public sealed class LicenseGuardService : BackgroundService
{
    private readonly LicenseValidator _validator;
    private readonly LicenseStore _store;
    private readonly TimeGuard _timeGuard;
    private readonly CloneDetector _cloneDetector;
    private readonly AntiTamper _antiTamper;
    private readonly OnlineLicenseClient _onlineClient;
    private readonly SecurityLogger _securityLogger;
    private readonly LicensingOptions _options;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<LicenseGuardService> _logger;

    private LicensePayload? _currentPayload;
    private LicenseState _currentState = new();
    private volatile bool _isLicenseValid;

    /// <summary>True, если лицензия действительна (или в Grace Period).</summary>
    public bool IsLicenseValid => _isLicenseValid;

    /// <summary>Текущий статус лицензии.</summary>
    public LicenseStatus CurrentStatus => _currentState.Status;

    /// <summary>Оставшееся время Grace Period (null если не в grace).</summary>
    public TimeSpan? GraceRemaining
    {
        get
        {
            if (_currentState.Status != LicenseStatus.GracePeriod)
                return null;

            var elapsed = DateTime.UtcNow - _currentState.GracePeriodStartUtc;
            var remaining = TimeSpan.FromDays(_options.GracePeriodDays) - elapsed;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
    }

    public LicenseGuardService(
        LicenseValidator validator,
        LicenseStore store,
        TimeGuard timeGuard,
        CloneDetector cloneDetector,
        AntiTamper antiTamper,
        OnlineLicenseClient onlineClient,
        SecurityLogger securityLogger,
        IOptions<LicensingOptions> options,
        IHostApplicationLifetime lifetime,
        ILogger<LicenseGuardService> logger)
    {
        _validator = validator;
        _store = store;
        _timeGuard = timeGuard;
        _cloneDetector = cloneDetector;
        _antiTamper = antiTamper;
        _onlineClient = onlineClient;
        _securityLogger = securityLogger;
        _options = options.Value;
        _lifetime = lifetime;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("LicenseGuard запущен");

        // Начальная проверка при старте
        var startupResult = await PerformFullCheck(stoppingToken);
        if (!startupResult)
        {
            _logger.LogCritical("Лицензия невалидна при запуске — остановка сервиса");
            _lifetime.StopApplication();
            return;
        }

        // Проверка anti-tamper при старте
        var tamperResult = _antiTamper.Verify();
        if (!tamperResult.IsValid)
        {
            _securityLogger.LogTamperDetected(tamperResult.Message);
            _logger.LogCritical("Обнаружена модификация сборок: {Message}", tamperResult.Message);
            _lifetime.StopApplication();
            return;
        }

        // Периодическая проверка
        var interval = TimeSpan.FromMinutes(_options.OnlineCheckIntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);
                await PerformFullCheck(stoppingToken);

                if (!_isLicenseValid)
                {
                    _logger.LogCritical("Лицензия стала невалидной — остановка через 60 секунд");
                    await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                    _lifetime.StopApplication();
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка в цикле проверки лицензии");
            }
        }
    }

    /// <summary>
    /// Полная проверка: файл → подпись → время → клонирование → онлайн → grace.
    /// </summary>
    private async Task<bool> PerformFullCheck(CancellationToken ct)
    {
        // 1. Загрузка лицензии и состояния
        var licenseFile = _store.LoadLicense();
        if (licenseFile == null)
        {
            _isLicenseValid = false;
            _currentState.Status = LicenseStatus.NotActivated;
            _securityLogger.LogInvalidLicense("Файл лицензии не найден");
            return false;
        }

        _currentState = _store.LoadState() ?? new LicenseState();

        // 2. Проверка подписи и срока
        var hwid = HardwareFingerprint.Generate();
        var validationResult = _validator.Validate(licenseFile, hwid);

        if (!validationResult.IsValid && validationResult.Status != LicenseStatus.Expired)
        {
            _isLicenseValid = false;
            _currentState.Status = validationResult.Status;
            _store.SaveState(_currentState);
            _securityLogger.LogInvalidLicense(validationResult.Message);
            return false;
        }

        _currentPayload = validationResult.Payload ?? _validator.VerifySignature(licenseFile);
        if (_currentPayload == null)
        {
            _isLicenseValid = false;
            return false;
        }

        // 3. Проверка времени
        var timeCheck = _timeGuard.Check(_currentState);
        if (!timeCheck.IsValid)
        {
            _securityLogger.LogTimeRollback(timeCheck.Reason);
            // Откат времени → переход в Grace (не мгновенная блокировка)
            EnterGracePeriodIfNeeded("Обнаружен откат времени");
        }

        // 4. Проверка клонирования
        var cloneCheck = _cloneDetector.Check(_currentPayload, _currentState);
        if (cloneCheck.NeedsInitialization)
        {
            _cloneDetector.Initialize(_currentState);
        }
        else if (!cloneCheck.IsValid)
        {
            _securityLogger.LogCloneDetected(cloneCheck.Reason);
            _isLicenseValid = false;
            _currentState.Status = LicenseStatus.CloneDetected;
            _store.SaveState(_currentState);
            return false;
        }

        // 5. Онлайн-проверка (heartbeat)
        var heartbeat = await _onlineClient.HeartbeatAsync(
            _currentPayload.LicenseId,
            _currentState.InstanceId,
            hwid,
            _currentState.LeaseToken,
            ct);

        if (heartbeat != null)
        {
            if (heartbeat.Revoked)
            {
                _securityLogger.LogInvalidLicense("Лицензия отозвана сервером");
                _isLicenseValid = false;
                _currentState.Status = LicenseStatus.Revoked;
                _store.SaveState(_currentState);
                return false;
            }

            if (heartbeat.CloneDetected)
            {
                _securityLogger.LogCloneDetected("Сервер обнаружил клонирование");
                _isLicenseValid = false;
                _currentState.Status = LicenseStatus.CloneDetected;
                _store.SaveState(_currentState);
                return false;
            }

            // Обновляем lease и серверное время
            _currentState.LeaseToken = heartbeat.NewLeaseToken;
            _currentState.LeaseExpiryUtc = heartbeat.LeaseExpiryUtc;
            _currentState.LastOnlineCheckUtc = DateTime.UtcNow;
            _currentState.ConsecutiveFailedOnlineChecks = 0;

            _timeGuard.UpdateTimestamps(_currentState, heartbeat.ServerTimeUtc);

            // Выход из Grace Period при успешном онлайн-чеке
            if (_currentState.Status == LicenseStatus.GracePeriod && validationResult.IsValid)
            {
                _currentState.Status = LicenseStatus.Active;
                _currentState.GracePeriodStartUtc = DateTime.MinValue;
                _logger.LogInformation("Выход из Grace Period — лицензия снова активна");
            }
        }
        else
        {
            // Онлайн-проверка не удалась
            _currentState.ConsecutiveFailedOnlineChecks++;
            _securityLogger.LogOnlineCheckFailed(
                $"Попытка #{_currentState.ConsecutiveFailedOnlineChecks}");

            if (_currentState.ConsecutiveFailedOnlineChecks >= _options.MaxFailedOnlineChecks)
            {
                EnterGracePeriodIfNeeded("Слишком много неудачных онлайн-проверок");
            }
        }

        // 6. Проверка Grace Period
        if (_currentState.Status == LicenseStatus.GracePeriod)
        {
            var elapsed = DateTime.UtcNow - _currentState.GracePeriodStartUtc;
            if (elapsed > TimeSpan.FromDays(_options.GracePeriodDays))
            {
                _securityLogger.LogGracePeriodExpired();
                _isLicenseValid = false;
                _currentState.Status = LicenseStatus.Expired;
                _store.SaveState(_currentState);
                return false;
            }

            _isLicenseValid = true;
        }
        else if (validationResult.IsValid)
        {
            _currentState.Status = LicenseStatus.Active;
            _isLicenseValid = true;
        }
        else
        {
            // Лицензия истекла — входим в Grace
            EnterGracePeriodIfNeeded("Срок действия лицензии истёк");
            _isLicenseValid = _currentState.Status == LicenseStatus.GracePeriod;
        }

        // Обновляем временные метки
        _timeGuard.UpdateTimestamps(_currentState);
        _store.SaveState(_currentState);

        return _isLicenseValid;
    }

    private void EnterGracePeriodIfNeeded(string reason)
    {
        if (_currentState.Status == LicenseStatus.GracePeriod)
            return;

        _currentState.Status = LicenseStatus.GracePeriod;
        _currentState.GracePeriodStartUtc = DateTime.UtcNow;
        _securityLogger.LogGracePeriodStarted(_options.GracePeriodDays);
        _logger.LogWarning("Переход в Grace Period ({Days} дней): {Reason}",
            _options.GracePeriodDays, reason);
    }
}

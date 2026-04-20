using KIT.GasStation.Licensing.Client;
using KIT.GasStation.Licensing.Models;
using KIT.GasStation.Licensing.Protection;
using KIT.GasStation.Licensing.Storage;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KIT.GasStation.Licensing.Core;

/// <summary>
/// Фоновый сервис: первичная активация → периодическая проверка → heartbeat → grace period.
/// Интегрируется как IHostedService. При невосстановимой ошибке останавливает хост.
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

    // TaskCompletionSource — позволяет другим сервисам дождаться завершения начальной проверки
    private readonly TaskCompletionSource _initialCheckCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private LicensePayload? _currentPayload;
    private LicenseState _currentState = new();
    private volatile bool _isLicenseValid;

    /// <summary>True, если лицензия действительна (или в Grace Period).</summary>
    public bool IsLicenseValid => _isLicenseValid;

    /// <summary>Текущий статус лицензии.</summary>
    public LicenseStatus CurrentStatus => _currentState.Status;

    /// <summary>
    /// Task, завершающийся после первичной проверки лицензии при старте.
    /// Позволяет другим сервисам (Worker, SignalR и т.д.) дождаться результата.
    /// </summary>
    public Task InitialCheckCompleted => _initialCheckCompleted.Task;

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

        try
        {
            // ── Режим разработки: полностью пропустить лицензирование ──────────────
            if (_options.DisableLicensing)
            {
                _logger.LogWarning(
                    "Лицензирование полностью отключено (DisableLicensing=true) — " +
                    "ТОЛЬКО для разработки! В Production установите false.");
                _isLicenseValid = true;
                _initialCheckCompleted.TrySetResult();
                return;
            }
            // ──────────────────────────────────────────────────────────────────────

            // Anti-tamper при старте (быстро, не требует сети)
            // В режиме разработки можно отключить через Licensing:DisableAntiTamper = true
            if (_options.DisableAntiTamper)
            {
                _logger.LogWarning("AntiTamper отключён (DisableAntiTamper=true) — только для разработки!");
            }
            else
            {
                var tamperResult = _antiTamper.Verify();
                if (!tamperResult.IsValid)
                {
                    _securityLogger.LogTamperDetected(tamperResult.Message);
                    _logger.LogCritical("Обнаружена модификация сборок: {Message}", tamperResult.Message);
                    _initialCheckCompleted.TrySetResult();
                    _lifetime.StopApplication();
                    return;
                }
            }

            // Шаг 1: попытка автоматической активации, если файла лицензии нет
            await TryAutoActivateAsync(stoppingToken);

            // Шаг 2: начальная полная проверка
            var startupOk = await PerformFullCheck(stoppingToken);

            // Сигнализируем другим сервисам что начальная проверка завершена
            _initialCheckCompleted.TrySetResult();

            if (!startupOk)
            {
                _logger.LogCritical(
                    "Лицензия невалидна при запуске (статус: {Status}) — остановка через 5 секунд",
                    _currentState.Status);
                // Небольшая задержка, чтобы логи успели записаться
                await Task.Delay(TimeSpan.FromSeconds(5), CancellationToken.None);
                _lifetime.StopApplication();
                return;
            }

            _logger.LogInformation("Лицензия проверена. Статус: {Status}", _currentState.Status);

            // Шаг 3: периодическая фоновая проверка
            var interval = TimeSpan.FromMinutes(_options.OnlineCheckIntervalMinutes);

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(interval, stoppingToken);

                await PerformFullCheck(stoppingToken);

                if (!_isLicenseValid)
                {
                    _logger.LogCritical(
                        "Лицензия стала невалидной (статус: {Status}) — остановка через 60 секунд",
                        _currentState.Status);
                    await Task.Delay(TimeSpan.FromSeconds(60), CancellationToken.None);
                    _lifetime.StopApplication();
                    return;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Нормальное завершение по stoppingToken
            _initialCheckCompleted.TrySetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Необработанная ошибка в LicenseGuardService");
            _initialCheckCompleted.TrySetResult();
        }
    }

    /// <summary>
    /// Автоматическая активация при первом запуске.
    /// Срабатывает, если в appsettings указан LicenseKey и локальный файл лицензии не найден.
    /// </summary>
    private async Task TryAutoActivateAsync(CancellationToken ct)
    {
        // Если файл лицензии уже есть — активация не нужна
        if (_store.LoadLicense() != null)
            return;

        if (string.IsNullOrEmpty(_options.LicenseKey))
        {
            _logger.LogWarning(
                "Файл лицензии не найден и LicenseKey не указан в конфигурации. " +
                "Добавьте Licensing:LicenseKey в appsettings.json для автоматической активации.");
            return;
        }

        _logger.LogInformation("Файл лицензии не найден — выполняю автоматическую активацию...");

        var hwid = HardwareFingerprint.Generate();

        // Загружаем или генерируем InstanceId
        var state = _store.LoadState() ?? new LicenseState();
        if (string.IsNullOrEmpty(state.InstanceId))
        {
            _cloneDetector.Initialize(state);
            _store.SaveState(state);
        }

        var response = await _onlineClient.ActivateAsync(
            _options.LicenseKey, hwid, state.InstanceId, ct);

        if (response == null || !response.Success || response.LicenseFile == null)
        {
            _logger.LogError("Автоматическая активация не удалась: {Message}",
                response?.Message ?? "Сервер лицензирования недоступен");
            return;
        }

        // Сохраняем файл лицензии
        _store.SaveLicense(response.LicenseFile);

        // Обновляем состояние с lease-данными от сервера
        state.LeaseToken = response.LeaseToken;
        state.LeaseExpiryUtc = response.LeaseExpiryUtc;
        state.LastOnlineCheckUtc = DateTime.UtcNow;
        state.ConsecutiveFailedOnlineChecks = 0;
        _timeGuard.UpdateTimestamps(state, response.ServerTimeUtc);
        _store.SaveState(state);

        _securityLogger.LogActivationSuccess(_options.LicenseKey);
        _logger.LogInformation("Лицензия успешно активирована и сохранена.");
    }

    /// <summary>
    /// Полная проверка: файл → подпись → время → клонирование → онлайн → grace.
    /// </summary>
    private async Task<bool> PerformFullCheck(CancellationToken ct)
    {
        var licenseFile = _store.LoadLicense();
        if (licenseFile == null)
        {
            _isLicenseValid = false;
            _currentState.Status = LicenseStatus.NotActivated;
            _securityLogger.LogInvalidLicense("Файл лицензии не найден");
            return false;
        }

        _currentState = _store.LoadState() ?? new LicenseState();

        // 1. Проверка подписи и срока
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

        // 2. Проверка времени
        var timeCheck = _timeGuard.Check(_currentState);
        if (!timeCheck.IsValid)
        {
            _securityLogger.LogTimeRollback(timeCheck.Reason);
            EnterGracePeriodIfNeeded("Обнаружен откат времени");
        }

        // 3. Проверка клонирования
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

        // 4. Онлайн heartbeat
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

            _currentState.LeaseToken = heartbeat.NewLeaseToken;
            _currentState.LeaseExpiryUtc = heartbeat.LeaseExpiryUtc;
            _currentState.LastOnlineCheckUtc = DateTime.UtcNow;
            _currentState.ConsecutiveFailedOnlineChecks = 0;
            _timeGuard.UpdateTimestamps(_currentState, heartbeat.ServerTimeUtc);

            if (_currentState.Status == LicenseStatus.GracePeriod && validationResult.IsValid)
            {
                _currentState.Status = LicenseStatus.Active;
                _currentState.GracePeriodStartUtc = DateTime.MinValue;
                _logger.LogInformation("Выход из Grace Period — лицензия активна");
            }
        }
        else
        {
            _currentState.ConsecutiveFailedOnlineChecks++;
            _securityLogger.LogOnlineCheckFailed(
                $"Попытка #{_currentState.ConsecutiveFailedOnlineChecks}");

            if (_currentState.ConsecutiveFailedOnlineChecks >= _options.MaxFailedOnlineChecks)
                EnterGracePeriodIfNeeded("Слишком много неудачных онлайн-проверок");
        }

        // 5. Итог: grace или active
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
            EnterGracePeriodIfNeeded("Срок действия лицензии истёк");
            _isLicenseValid = _currentState.Status == LicenseStatus.GracePeriod;
        }

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

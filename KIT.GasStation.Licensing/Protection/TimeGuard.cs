using KIT.GasStation.Licensing.Models;
using KIT.GasStation.Licensing.Storage;
using Microsoft.Extensions.Logging;

namespace KIT.GasStation.Licensing.Protection;

/// <summary>
/// Детектор манипуляций с системным временем.
/// Использует комбинацию: монотонный счётчик + серверное время + дельта.
/// </summary>
public sealed class TimeGuard
{
    private readonly LicenseStore _store;
    private readonly ILogger<TimeGuard> _logger;

    /// <summary>Максимально допустимое отклонение назад (5 минут).</summary>
    private static readonly TimeSpan MaxAllowedBackwardDrift = TimeSpan.FromMinutes(5);

    /// <summary>Максимально допустимое отклонение между монотонным и реальным временем (2 часа).</summary>
    private static readonly TimeSpan MaxMonotonicDrift = TimeSpan.FromHours(2);

    public TimeGuard(LicenseStore store, ILogger<TimeGuard> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>
    /// Проверяет, не было ли отката системного времени.
    /// </summary>
    public TimeCheckResult Check(LicenseState state)
    {
        var now = DateTime.UtcNow;
        var currentTick = Environment.TickCount64;

        // 1. Проверка: текущее время не раньше последнего зафиксированного
        if (state.LastLocalTimeUtc > DateTime.MinValue)
        {
            var backward = state.LastLocalTimeUtc - now;
            if (backward > MaxAllowedBackwardDrift)
            {
                _logger.LogWarning(
                    "Обнаружен откат времени: текущее {Now}, последнее {Last}, дельта {Delta}",
                    now, state.LastLocalTimeUtc, backward);

                return new TimeCheckResult
                {
                    IsValid = false,
                    Reason = $"Системное время откатано на {backward.TotalMinutes:F0} минут"
                };
            }
        }

        // 2. Проверка монотонного счётчика vs реального времени
        if (state.LastTickCount > 0)
        {
            var tickElapsed = TimeSpan.FromMilliseconds(currentTick - state.LastTickCount);
            var timeElapsed = now - state.LastLocalTimeUtc;

            // Если TickCount ушёл далеко вперёд, а время — нет → время было переведено назад
            if (tickElapsed > TimeSpan.Zero && timeElapsed > TimeSpan.Zero)
            {
                var drift = tickElapsed - timeElapsed;
                if (drift.Duration() > MaxMonotonicDrift)
                {
                    _logger.LogWarning(
                        "Рассинхрон монотонного счётчика и времени: tick={TickElapsed}, time={TimeElapsed}",
                        tickElapsed, timeElapsed);

                    return new TimeCheckResult
                    {
                        IsValid = false,
                        Reason = "Обнаружено расхождение монотонного времени и системных часов"
                    };
                }
            }
        }

        // 3. Проверка: текущее время не раньше серверного (если есть)
        if (state.LastServerTimeUtc > DateTime.MinValue)
        {
            var serverDrift = state.LastServerTimeUtc - now;
            if (serverDrift > MaxAllowedBackwardDrift)
            {
                _logger.LogWarning(
                    "Текущее время {Now} раньше последнего серверного {Server}",
                    now, state.LastServerTimeUtc);

                return new TimeCheckResult
                {
                    IsValid = false,
                    Reason = "Системное время раньше серверного — возможна подмена"
                };
            }
        }

        return new TimeCheckResult { IsValid = true };
    }

    /// <summary>
    /// Обновляет временные метки в состоянии.
    /// </summary>
    public void UpdateTimestamps(LicenseState state, DateTime? serverTime = null)
    {
        state.LastLocalTimeUtc = DateTime.UtcNow;
        state.LastTickCount = Environment.TickCount64;

        if (serverTime.HasValue)
            state.LastServerTimeUtc = serverTime.Value;
    }
}

public sealed class TimeCheckResult
{
    public bool IsValid { get; init; }
    public string Reason { get; init; } = string.Empty;
}

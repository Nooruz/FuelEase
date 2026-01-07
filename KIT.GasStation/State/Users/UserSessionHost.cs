using KIT.GasStation.Domain.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KIT.GasStation.State.Users
{
    public sealed class UserSessionHost : IDisposable
    {
        private readonly IUserStore _userStore;
        private readonly IEnumerable<IUserSessionService> _services;
        private readonly ILogger _logger;

        private CancellationTokenSource? _sessionCts;
        private bool _started;

        public UserSessionHost(IUserStore userStore, IEnumerable<IUserSessionService> services, ILogger logger)
        {
            _userStore = userStore;
            _services = services;
            _logger = logger;

            _userStore.OnLogin += HandleLogin;
            _userStore.OnLogout += HandleLogout;
        }

        public void Start()
        {
            _started = true;

            // Если пользователь уже залогинен (например, восстановили сессии) — стартанём
            if (_userStore.CurrentUser != null)
                HandleLogin(_userStore.CurrentUser);
        }

        private async void HandleLogin(User user)
        {
            if (!_started) return;

            // гасим предыдущую сессию, если вдруг логин поверх логина
            await StopServicesAsync().ConfigureAwait(false);

            _sessionCts = new CancellationTokenSource();
            var ct = _sessionCts.Token;

            foreach (var s in _services)
            {
                try
                {
                    await s.OnLoginAsync(user, ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Ошибка запуска сервиса {s.GetType().Name} при логине.");
                    // решай сам: либо продолжаем, либо валим всё
                }
            }
        }

        private async void HandleLogout()
        {
            if (!_started) return;
            await StopServicesAsync().ConfigureAwait(false);
        }

        private async Task StopServicesAsync()
        {
            if (_sessionCts is null) return;

            try { _sessionCts.Cancel(); } catch { /* похуй */ }

            // стопаем в обратном порядке — как по канону
            foreach (var s in _services.Reverse())
            {
                try
                {
                    await s.OnLogoutAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Ошибка остановки сервиса {s.GetType().Name} при логауте.");
                }
            }

            _sessionCts.Dispose();
            _sessionCts = null;
        }

        public void Dispose()
        {
            _userStore.OnLogin -= HandleLogin;
            _userStore.OnLogout -= HandleLogout;

            // best-effort
            try { StopServicesAsync().GetAwaiter().GetResult(); } catch { }
        }
    }
}

using KIT.GasStation.EKassa.Models;

namespace KIT.GasStation.EKassa
{
    public sealed class EkassaTokenStore
    {
        private readonly SemaphoreSlim _lock = new(1, 1);

        private string? _accessToken;
        private DateTimeOffset _issuedAt;

        public string? AccessToken => _accessToken;

        public bool HasToken => !string.IsNullOrWhiteSpace(_accessToken);

        public void SetToken(AuthLoginData data)
        {
            _accessToken = data.AccessToken;
            _issuedAt = DateTimeOffset.UtcNow;
        }

        public void Clear()
        {
            _accessToken = null;
            _issuedAt = default;
        }

        /// <summary>
        /// Гарантирует что токен будет обновлён один раз на пачку параллельных запросов.
        /// </summary>
        public async Task EnsureTokenAsync(Func<CancellationToken, Task<AuthLoginData>> loginFunc, CancellationToken ct)
        {
            if (HasToken) return;

            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (HasToken) return;
                var data = await loginFunc(ct).ConfigureAwait(false);
                SetToken(data);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task RefreshTokenAsync(Func<CancellationToken, Task<AuthLoginData>> loginFunc, CancellationToken ct)
        {
            await _lock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // Всегда обновляем (после 401)
                var data = await loginFunc(ct).ConfigureAwait(false);
                SetToken(data);
            }
            finally
            {
                _lock.Release();
            }
        }
    }
}

using System.Net;
using System.Net.Http.Headers;

namespace KIT.GasStation.EKassa
{
    internal sealed class EkassaAuthHandler : DelegatingHandler
    {
        private readonly EkassaTokenStore _tokenStore;
        private readonly EkassaOptions _options;
        private readonly EkassaLoginApi _loginApi;

        public EkassaAuthHandler(EkassaTokenStore tokenStore, EkassaOptions options, EkassaLoginApi loginApi)
        {
            _tokenStore = tokenStore;
            _options = options;
            _loginApi = loginApi;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            // 1) Если токена нет — логинимся один раз
            await _tokenStore.EnsureTokenAsync(_loginApi.LoginAsync, ct).ConfigureAwait(false);

            // 2) Добавляем Authorization
            AttachBearer(request);

            // 3) Отправляем
            var response = await base.SendAsync(request, ct).ConfigureAwait(false);
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                return response;

            // 4) 401 — обновляем токен и повторяем ограниченное число раз
            response.Dispose();

            for (int i = 0; i < _options.MaxAuthRetry; i++)
            {
                await _tokenStore.RefreshTokenAsync(_loginApi.LoginAsync, ct).ConfigureAwait(false);

                // HttpRequestMessage нельзя переиспользовать, если был контент-стрим.
                // Поэтому требование: запросы создаём заново в EkassaClient через Func<HttpRequestMessage>.
                // Здесь handler уже получил конкретный request — повтор можно делать только если контент буферизован.
                // Мы делаем проще: запрещаем повтор тут.
                break;
            }

            // Возвращаем 401 как есть — повтор реализуем на уровне EkassaClient, где можно пересоздать запрос.
            return new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                RequestMessage = request,
                ReasonPhrase = "Unauthorized (token refreshed; retry required)"
            };
        }

        private void AttachBearer(HttpRequestMessage request)
        {
            var token = _tokenStore.AccessToken;
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}

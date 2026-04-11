using System.Net;
using System.Net.Http.Headers;

namespace KIT.GasStation.EKassa
{
    internal sealed class EkassaAuthHandler : DelegatingHandler
    {
        private readonly EkassaTokenStore _tokenStore;
        private readonly EkassaLoginApi _loginApi;

        public EkassaAuthHandler(EkassaTokenStore tokenStore, EkassaLoginApi loginApi)
        {
            _tokenStore = tokenStore;
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

            // Обновление токена и повтор делаются на уровне EkassaClient,
            // чтобы избежать двойного login (важно: новый login инвалидирует прошлый токен по документации).
            return response;
        }

        private void AttachBearer(HttpRequestMessage request)
        {
            var token = _tokenStore.AccessToken;
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}

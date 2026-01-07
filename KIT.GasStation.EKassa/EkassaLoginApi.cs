using KIT.GasStation.EKassa.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace KIT.GasStation.EKassa
{
    public sealed class EkassaLoginApi
    {
        private readonly HttpClient _http;
        private readonly EkassaOptions _options;
        private static readonly JsonSerializerOptions JsonOptions = EkassaJson.Options;

        public EkassaLoginApi(HttpClient http, EkassaOptions options)
        {
            _http = http;
            _options = options;
        }

        public async Task<AuthLoginData> LoginAsync(CancellationToken ct)
        {
            var req = new AuthLoginRequest { Email = _options.Email, Password = _options.Password };

            using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(req, options: JsonOptions)
            };

            using var resp = await _http.SendAsync(msg, ct).ConfigureAwait(false);
            var payload = await resp.Content.ReadFromJsonAsync<EkassaResponse<AuthLoginData>>(JsonOptions, ct)
                          .ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode || payload?.Data is null)
                throw EkassaHttpException.From(resp, payload);

            return payload.Data;
        }
    }
}

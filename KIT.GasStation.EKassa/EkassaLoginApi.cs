using KIT.GasStation.EKassa.Models;
using System.Net.Http.Headers;
using System.Text;
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
            var json = JsonSerializer.Serialize(req, JsonOptions);
            var content = new StringContent(json, Encoding.UTF8);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using var msg = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = content
            };
            msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await _http.SendAsync(msg, ct).ConfigureAwait(false);
            var raw = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            EkassaResponse<AuthLoginData>? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<EkassaResponse<AuthLoginData>>(raw, JsonOptions);
            }
            catch
            {
                // Оставляем payload = null, исключение ниже будет с исходным HTTP-кодом.
            }

            if (!resp.IsSuccessStatusCode || payload?.Data is null)
                throw EkassaHttpException.From(resp, payload);

            return payload.Data;
        }
    }
}

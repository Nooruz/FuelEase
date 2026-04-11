using KIT.GasStation.EKassa.Models;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace KIT.GasStation.EKassa.Services
{
    public sealed class EkassaClient : IEkassaClient
    {
        private readonly HttpClient _http;              // с EkassaAuthHandler
        private readonly EkassaLoginApi _loginApi;      // без EkassaAuthHandler
        private readonly EkassaTokenStore _tokenStore;
        private readonly EkassaOptions _options;

        public EkassaClient(HttpClient http, EkassaLoginApi loginApi, EkassaTokenStore tokenStore, EkassaOptions options)
        {
            _http = http;
            _loginApi = loginApi;
            _tokenStore = tokenStore;
            _options = options;
        }

        public Task<AuthLoginData> LoginAsync(CancellationToken ct = default) => _loginApi.LoginAsync(ct);

        public Task<PosInfoData> GetPosByFiscalNumberAsync(GetPosByFiscalNumberRequest request, CancellationToken ct = default)
            => PostWithRetryAsync<PosInfoData>("/api/get_pos_by_fiscal_number", request, ct);

        public Task<BaseCatalogueData> GetBaseCatalogueByFiscalNumberAsync(GetBaseCatalogueByFiscalNumberRequest request, CancellationToken ct = default)
            => PostWithRetryAsync<BaseCatalogueData>("/api/get_base_catalogue_by_fiscal_number", request, ct);

        public Task<ShiftReportData> ShiftOpenAsync(ShiftOpenRequest request, CancellationToken ct = default)
            => PostWithRetryAsync<ShiftReportData>("/api/shift_open_by_fiscal_number", request, ct);

        public Task<ShiftReportData> ShiftCloseAsync(ShiftCloseRequest request, CancellationToken ct = default)
            => PostWithRetryAsync<ShiftReportData>("/api/shift_close_by_fiscal_number", request, ct);

        public Task<ShiftReportData> ShiftStateAsync(ShiftStateRequest request, CancellationToken ct = default)
            => PostWithRetryAsync<ShiftReportData>("/api/shift_state_by_fiscal_number", request, ct);

        public Task<ReceiptData> CreateReceiptV2Async(ReceiptV2Request request, CancellationToken ct = default)
            => PostWithRetryAsync<ReceiptData>("/api/v2/receipt", request, ct);

        public Task<CashOperationData> CashOperationAsync(CashOperationRequest request, CancellationToken ct = default)
            => PostWithRetryAsync<CashOperationData>("/api/cash_operation_by_fiscal_number", request, ct);

        public Task<DuplicateReceiptData> DuplicateReceiptAsync(DuplicateReceiptRequest request, CancellationToken ct = default)
            => PostWithRetryAsync<DuplicateReceiptData>("/api/duplicate", request, ct);

        private async Task<TData> PostWithRetryAsync<TData>(string path, object body, CancellationToken ct)
        {
            // 1-я попытка
            var r1 = await SendPostAsync<TData>(path, body, ct).ConfigureAwait(false);
            if (r1.IsOk) return r1.Data!;

            // Если 401 — делаем релогин и повтор
            if (r1.StatusCode == HttpStatusCode.Unauthorized)
            {
                // Единственное место повторной авторизации.
                // В eKassa новый login инвалидирует ранее выданный токен.
                await _tokenStore.RefreshTokenAsync(_loginApi.LoginAsync, ct).ConfigureAwait(false);

                var r2 = await SendPostAsync<TData>(path, body, ct).ConfigureAwait(false);
                if (r2.IsOk) return r2.Data!;
                throw r2.Error!;
            }

            throw r1.Error!;
        }

        private async Task<(bool IsOk, TData? Data, EkassaHttpException? Error, HttpStatusCode StatusCode)> SendPostAsync<TData>(
            string path, object body, CancellationToken ct)
        {
            var content = JsonContent.Create(body, body.GetType(), options: EkassaJson.Options);

            using var msg = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = content
            };

            msg.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (msg.Headers.Authorization is null && !string.IsNullOrWhiteSpace(_tokenStore.AccessToken))
            {
                msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenStore.AccessToken);
            }

            // ===== ОТЛАДКА =====
            var requestBody = await msg.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            Debug.WriteLine("=== eKassa REQUEST ===");
            Debug.WriteLine($"URI: {msg.RequestUri}");
            Debug.WriteLine($"Accept: {string.Join(", ", msg.Headers.Accept)}");
            Debug.WriteLine($"Authorization: {msg.Headers.Authorization}");
            Debug.WriteLine($"Content-Type: {msg.Content.Headers.ContentType}");
            Debug.WriteLine("BODY:");
            Debug.WriteLine(requestBody);
            Debug.WriteLine("======================");
            // ===================

            using var resp = await _http.SendAsync(msg, ct).ConfigureAwait(false);

            var rawResponse = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            Debug.WriteLine("=== eKassa RESPONSE ===");
            Debug.WriteLine($"HTTP: {(int)resp.StatusCode} {resp.StatusCode}");
            Debug.WriteLine(rawResponse);
            Debug.WriteLine("=======================");

            EkassaResponse<TData>? payload = null;
            try
            {
                payload = JsonSerializer.Deserialize<EkassaResponse<TData>>(rawResponse, EkassaJson.Options);
            }
            catch
            {
                // если eKassa вернула не JSON, оставим payload = null
            }

            if (resp.IsSuccessStatusCode
                && payload is not null
                && string.Equals(payload.Status, "Success", StringComparison.OrdinalIgnoreCase)
                && payload.Data is not null)
                return (true, payload.Data, null, resp.StatusCode);

            if (resp.IsSuccessStatusCode && payload is not null
                && string.Equals(payload.Status, "Error", StringComparison.OrdinalIgnoreCase))
            {
                string? bodyCode = null;
                if (payload.Message.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        var em = payload.Message.Deserialize<EkassaErrorMessage>(EkassaJson.Options);
                        bodyCode = em?.Code;
                    }
                    catch { }
                }

                if (bodyCode != null && int.TryParse(bodyCode, out var bodyHttpCode))
                {
                    var fakeResp = new HttpResponseMessage((HttpStatusCode)bodyHttpCode);
                    var ex2 = EkassaHttpException.From(fakeResp, payload);
                    return (false, default, ex2, (HttpStatusCode)bodyHttpCode);
                }
            }

            var ex = EkassaHttpException.From(resp, payload);
            return (false, default, ex, resp.StatusCode);
        }
    }
}
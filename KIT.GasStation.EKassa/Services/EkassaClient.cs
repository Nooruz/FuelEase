using KIT.GasStation.EKassa.Models;
using System.Net;
using System.Net.Http.Json;

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
                // принудительно обновим токен
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
            using var msg = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = JsonContent.Create(body, options: EkassaJson.Options)
            };

            using var resp = await _http.SendAsync(msg, ct).ConfigureAwait(false);

            var payload = await resp.Content.ReadFromJsonAsync<EkassaResponse<TData>>(EkassaJson.Options, ct)
                          .ConfigureAwait(false);

            if (resp.IsSuccessStatusCode && payload.Data is not null)
                return (true, payload.Data, null, resp.StatusCode);

            var ex = EkassaHttpException.From(resp, payload);
            return (false, default, ex, resp.StatusCode);
        }
    }
}

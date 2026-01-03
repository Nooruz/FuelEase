using KIT.GasStation.EKassa.Models;
using System.Net;
using System.Text.Json;

namespace KIT.GasStation.EKassa
{
    public sealed class EkassaHttpException : Exception
    {
        public HttpStatusCode StatusCode { get; }
        public string? EkassaCode { get; }
        public string? EkassaError { get; }

        private EkassaHttpException(HttpStatusCode statusCode, string message, string? ekassaCode, string? ekassaError)
            : base(message)
        {
            StatusCode = statusCode;
            EkassaCode = ekassaCode;
            EkassaError = ekassaError;
        }

        public static EkassaHttpException From<T>(HttpResponseMessage resp, EkassaResponse<T>? payload)
        {
            string? code = null;
            string? err = null;

            if (payload is not null)
            {
                // message может быть строкой или объектом {code, error}
                if (payload.Message.ValueKind == JsonValueKind.Object)
                {
                    try
                    {
                        var em = payload.Message.Deserialize<EkassaErrorMessage>(EkassaJson.Options);
                        code = em?.Code;
                        err = em?.Error;
                    }
                    catch { /* игнор */ }
                }
                else if (payload.Message.ValueKind == JsonValueKind.String)
                {
                    err = payload.Message.GetString();
                }
            }

            var msg = $"eKassa HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}"
                      + (string.IsNullOrWhiteSpace(err) ? "" : $": {err}");

            return new EkassaHttpException(resp.StatusCode, msg, code, err);
        }
    }
}

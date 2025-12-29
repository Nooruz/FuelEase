using System.Text.Json.Serialization;

namespace KIT.GasStation.NewCas.Models
{
    public class OpenAndCloseRecResp
    {
        [JsonPropertyName("status")]
        public OpenAndCloseRecRespStatus Status { get; set; }

        [JsonPropertyName("extCode")]
        public int ExtCode { get; set; }

        [JsonPropertyName("extCode2")]
        public int ExtCode2 { get; set; }

        [JsonPropertyName("errorMessage")]
        public string? ErrorMessage { get; set; }

        [JsonPropertyName("qrCode")]
        public string? QRCode { get; set; }

        [JsonPropertyName("tin")]
        public string? TIN { get; set; }

        [JsonPropertyName("registrationNumber")]
        public string? RegistrationNumber { get; set; }

        [JsonPropertyName("fmNumber")]
        public string? FMNumber { get; set; }

        [JsonPropertyName("shiftNumber")]
        public int ShiftNumber { get; set; }

        [JsonPropertyName("fdNumber")]
        public int FDNumber { get; set; }

        [JsonPropertyName("dateTime")]
        public string? DateTime { get; set; }

        [JsonPropertyName("bitmaps")]
        public string[]? Bitmaps { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record AuthLoginData
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; init; } = default!;

        [JsonPropertyName("token_type")]
        public string TokenType { get; init; } = default!; // "Bearer"
    }
}

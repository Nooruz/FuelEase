using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record AuthLoginRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; init; } = default!;

        [JsonPropertyName("password")]
        public string Password { get; init; } = default!;
    }
}

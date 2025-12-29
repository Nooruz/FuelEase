using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public class LoginResult
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; }
    }
}

using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public class Login
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }
    }
}

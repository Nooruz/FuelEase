using System.Text.Json;
using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa
{
    internal static class EkassaJson
    {
        public static readonly JsonSerializerOptions Options = Create();

        private static JsonSerializerOptions Create()
        {
            var o = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            o.Converters.Add(new JsonStringEnumConverter());
            return o;
        }
    }
}

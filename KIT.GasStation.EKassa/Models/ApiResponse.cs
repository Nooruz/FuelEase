using KIT.GasStation.CashRegisters.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public class ApiResponse<T>
    {
        [JsonPropertyName("status")]
        public string StatusRaw { get; set; }

        [JsonIgnore]
        public ResponseStatus Status
        {
            get
            {
                if (Enum.TryParse<ResponseStatus>(StatusRaw, true, out var result))
                    return result;

                throw new CashRegisterException($"Неизвестный статус ответа: {StatusRaw}");
            }
        }

        // может быть string ИЛИ объект
        [JsonPropertyName("message")]
        public JsonElement Message { get; set; }

        [JsonPropertyName("data")]
        public T Data { get; set; }

        public string GetMessageText()
        {
            try
            {
                return Message.ValueKind switch
                {
                    JsonValueKind.String => Message.GetString() ?? "",
                    JsonValueKind.Object => Message.TryGetProperty("error", out var err) ? (err.GetString() ?? "") :
                                           Message.TryGetProperty("message", out var msg) ? (msg.GetString() ?? "") :
                                           Message.GetRawText(),
                    JsonValueKind.Array => Message.GetRawText(),
                    JsonValueKind.Null => "",
                    _ => Message.GetRawText()
                };
            }
            catch
            {
                return "";
            }
        }
    }
}

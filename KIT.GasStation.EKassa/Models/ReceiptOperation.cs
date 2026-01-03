using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ReceiptOperation
    {
        INCOME,
        INCOME_RETURN,
        EXPENDITURE,
        EXPENDITURE_RETURN
    }
}

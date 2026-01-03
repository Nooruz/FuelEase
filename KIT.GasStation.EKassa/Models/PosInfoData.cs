using System.Text.Json;
using System.Text.Json.Serialization;

namespace KIT.GasStation.EKassa.Models
{
    public sealed record PosInfoData
    {
        [JsonPropertyName("uuid")] public string? Uuid { get; init; }
        [JsonPropertyName("name")] public string? Name { get; init; }
        [JsonPropertyName("tin")] public string? Tin { get; init; }
        [JsonPropertyName("cashiername")] public string? CashierName { get; init; }
        [JsonPropertyName("storename")] public string? StoreName { get; init; }

        [JsonPropertyName("fnregistrationnumber")] public string? FnRegistrationNumber { get; init; }
        [JsonPropertyName("fnexpiresat")] public string? FnExpiresAt { get; init; }
        [JsonPropertyName("fnregistrationprogress")] public string? FnRegistrationProgress { get; init; }

        [JsonPropertyName("ccrmodel")] public string? CcrModel { get; init; }
        [JsonPropertyName("ccrversion")] public string? CcrVersion { get; init; }

        [JsonPropertyName("kkmregistrationdate")] public string? KkmRegistrationDate { get; init; }
        [JsonPropertyName("kkmserialnumber")] public string? KkmSerialNumber { get; init; }
        [JsonPropertyName("kkmregistrationprogress")] public string? KkmRegistrationProgress { get; init; }
        [JsonPropertyName("kkmfiscalnumber")] public string? KkmFiscalNumber { get; init; }
        [JsonPropertyName("kkmofdstatus")] public string? KkmOfdStatus { get; init; }

        [JsonPropertyName("administrativearea")] public string? AdministrativeArea { get; init; }
        [JsonPropertyName("locality")] public string? Locality { get; init; }
        [JsonPropertyName("postalcode")] public string? PostalCode { get; init; }
        [JsonPropertyName("route")] public string? Route { get; init; }
        [JsonPropertyName("streetnumber")] public string? StreetNumber { get; init; }
        [JsonPropertyName("location")] public string? Location { get; init; }

        [JsonPropertyName("taxsystems")] public int? TaxSystems { get; init; }
        [JsonPropertyName("calcitemattributes")] public int? CalcItemAttributes { get; init; }
        [JsonPropertyName("calcItemAttributeCode")] public string? CalcItemAttributeCode { get; init; }

        [JsonPropertyName("entrepreneurshipobject")] public int? EntrepreneurshipObject { get; init; }
        [JsonPropertyName("businessactivity")] public int? BusinessActivity { get; init; }
        [JsonPropertyName("taxauthoritydepartment")] public int? TaxAuthorityDepartment { get; init; }

        // Налоги по умолчанию для кассы (могут пригодиться при чеке)
        [JsonPropertyName("is_vat")] public int? IsVat { get; init; }
        [JsonPropertyName("set_vat_code")] public int? SetVatCode { get; init; }
        [JsonPropertyName("set_vat")] public string? SetVat { get; init; }
        [JsonPropertyName("set_st_c_code")] public int? SetStCashCode { get; init; }
        [JsonPropertyName("set_st_c")] public string? SetStCash { get; init; }
        [JsonPropertyName("set_st_b_code")] public int? SetStCashlessCode { get; init; }
        [JsonPropertyName("set_st_b")] public string? SetStCashless { get; init; }

        // Смена/счетчики (в PDF много полей, оставим основные; остальное добирается ExtensionData)
        [JsonPropertyName("shift")] public int? Shift { get; init; }
        [JsonPropertyName("shift_state")] public int? ShiftState { get; init; }
        [JsonPropertyName("shift_date")] public string? ShiftDate { get; init; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement> Extra { get; init; } = new();
    }
}

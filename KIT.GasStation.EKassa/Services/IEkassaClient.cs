using KIT.GasStation.EKassa.Models;

namespace KIT.GasStation.EKassa.Services
{
    public interface IEkassaClient
    {
        Task<AuthLoginData> LoginAsync(CancellationToken ct = default);

        Task<PosInfoData> GetPosByFiscalNumberAsync(
            GetPosByFiscalNumberRequest request, CancellationToken ct = default);

        Task<BaseCatalogueData> GetBaseCatalogueByFiscalNumberAsync(
            GetBaseCatalogueByFiscalNumberRequest request, CancellationToken ct = default);

        Task<ShiftReportData> ShiftOpenAsync(ShiftOpenRequest request, CancellationToken ct = default);
        Task<ShiftReportData> ShiftCloseAsync(ShiftCloseRequest request, CancellationToken ct = default);
        Task<ShiftReportData> ShiftStateAsync(ShiftStateRequest request, CancellationToken ct = default);

        Task<ReceiptData> CreateReceiptV2Async(ReceiptV2Request request, CancellationToken ct = default);

        Task<CashOperationData> CashOperationAsync(CashOperationRequest request, CancellationToken ct = default);

        Task<DuplicateReceiptData> DuplicateReceiptAsync(DuplicateReceiptRequest request, CancellationToken ct = default);
    }
}

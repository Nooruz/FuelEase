using KIT.GasStation.CashRegisters.Models;
using KIT.GasStation.Domain.Models.CashRegisters;

namespace KIT.GasStation.CashRegisters.Services
{

    public delegate TCashRegister CreateCashRegister<TCashRegister>() where TCashRegister : ICashRegisterService;
    public interface ICashRegisterService
    {
        #region Voids

        /// <summary>
        /// Инициализирует кассу.
        /// </summary>
        /// <returns></returns>
        Task InitializationAsync(Guid cashRegisterId);

        /// <summary>
        /// Открывает смену.
        /// </summary>
        Task OpenShiftAsync(string cashierName);

        /// <summary>
        /// Закрывает смену.
        /// </summary>
        Task CloseShiftAsync(string cashierName);

        /// <summary>
        /// Проводит операцию продажи.
        /// </summary>
        Task<FiscalData?> SaleAsync(FiscalData fiscalData, string cashierName);

        /// <summary>
        /// Х-отчет.
        /// </summary>
        Task XReportAsync(bool printReceipt = true);

        /// <summary>
        /// Возврат
        /// </summary>
        Task<FiscalData?> ReturnAsync(FiscalData fiscalData);

        /// <summary>
        /// Получение статуса ККМ
        /// </summary>
        /// <returns></returns>
        Task<CashRegisterState> GetShiftStateAsync();

        /// <summary>
        /// Получить отчёт по продажам за текущую смену (нал, безнал, возвраты).
        /// </summary>
        Task<ShiftSalesReport> GetShiftSalesReportAsync();

        #endregion
    }
}

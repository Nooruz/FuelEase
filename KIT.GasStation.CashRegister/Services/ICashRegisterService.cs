using KIT.GasStation.Domain.Models;
using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.CashRegisters.Services
{

    public delegate TCashRegister CreateCashRegister<TCashRegister>() where TCashRegister : ICashRegisterService;
    public interface ICashRegisterService
    {
        #region Actions

        event Action OnShiftOpened;
        event Action OnShiftClosed;
        event Action OnReceiptPrinting;
        event Action<FuelSale> OnReturning;
        event Action<string> OnUnknownError;
        event Action<CashRegisterStatus> OnStatusChanged;

        #endregion

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
        Task<FiscalData?> SaleAsync(FuelSale fuelSale, Fuel fuel, string cashierName);

        /// <summary>
        /// Х-отчет.
        /// </summary>
        Task XReportAsync(bool printReceipt = true);

        /// <summary>
        /// Возврат
        /// </summary>
        Task ReturnAsync(FuelSale fuelSale, Fuel fuel);

        /// <summary>
        /// Получение статуса ККМ
        /// </summary>
        /// <returns></returns>
        Task<string?> GetShiftStateAsync();

        /// <summary>
        /// Возврат и продажа по полученными суммами
        /// </summary>
        Task ReturnAndReceivedSaleAsync(FuelSale fuelSale, Fuel fuel);

        #endregion
    }
}

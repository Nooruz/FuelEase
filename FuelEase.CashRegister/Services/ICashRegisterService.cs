using FuelEase.Domain.Models;
using FuelEase.HardwareConfigurations.Models;

namespace FuelEase.CashRegisters.Services
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
        Task OpenShiftAsync();

        /// <summary>
        /// Закрывает смену.
        /// </summary>
        Task CloseShiftAsync();

        /// <summary>
        /// Проводит операцию продажи.
        /// </summary>
        Task SaleAsync(FuelSale fuelSale, Fuel fuel);

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

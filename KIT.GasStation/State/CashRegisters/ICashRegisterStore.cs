using KIT.GasStation.Domain.Models;
using KIT.GasStation.HardwareConfigurations.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;

namespace KIT.GasStation.State.CashRegisters
{
    /// <summary>
    /// Хранилище ККМ
    /// </summary>
    public interface ICashRegisterStore : IHostedService, INotifyPropertyChanged
    {
        event Action OnReceiptPrinting;
        event Action OnShiftOpened;
        event Action OnShiftClosed;
        event Action<FuelSale> OnReturning;
        event Action<string> OnUnknownError;
        event Action<CashRegisterStatus> OnStatusChanged;

        CashRegister CashRegister { get; }
        ObservableCollection<CashRegister> CashRegisters { get; }

        void ChangeDefaultCashRegister(Guid guid);

        /// <summary>
        /// Открыть смену
        /// </summary>
        /// <returns></returns>
        Task OpenShiftAsync();

        /// <summary>
        /// Закрыть смену
        /// </summary>
        /// <returns></returns>
        Task CloseShiftAsync();

        /// <summary>
        /// X отчет
        /// </summary>
        /// <returns></returns>
        Task XReportAsync();

        /// <summary>
        /// Получение статуса ККМ
        /// </summary>
        /// <returns></returns>
        Task<string?> GetShiftStateAsync();

        /// <summary>
        /// Продажа
        /// </summary>
        Task<FiscalData?> SaleAsync(FuelSale fuelSale, Fuel fuel);

        /// <summary>
        /// Возврат
        /// </summary>
        Task<FiscalData?> ReturnAsync(FuelSale fuelSale, Fuel fuel);

        /// <summary>
        /// Возврат и продажа по полученными суммами
        /// </summary>
        Task<FiscalData?> ReturnAndReceivedSaleAsync(FuelSale fuelSale, Fuel fuel, string cashierName);
    }
}

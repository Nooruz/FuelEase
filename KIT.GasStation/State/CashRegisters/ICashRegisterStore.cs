using KIT.GasStation.CashRegisters.Models;
using KIT.GasStation.Domain.Models.CashRegisters;
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
        CashRegister CashRegister { get; }
        ObservableCollection<CashRegister> CashRegisters { get; }

        public CashRegisterStatus Status { get; }
        public DateTime? OpenAt { get; }
        public DateTime? CloseAt { get; }

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
        Task<CashRegisterState> GetShiftStateAsync();

        /// <summary>
        /// Отчёт по продажам за текущую смену ККМ (нал, безнал, возвраты).
        /// Обновляется при запуске и после каждой продажи.
        /// </summary>
        ShiftSalesReport? ShiftSalesReport { get; }

        string ShiftStateMessage { get; }

        /// <summary>
        /// Получить отчёт по продажам за текущую смену.
        /// </summary>
        Task<ShiftSalesReport> GetShiftSalesReportAsync();

        /// <summary>
        /// Продажа
        /// </summary>
        Task<FiscalData?> SaleAsync(FiscalData fiscalData);

        /// <summary>
        /// Возврат
        /// </summary>
        Task<FiscalData?> ReturnAsync(FiscalData fiscalData);
    }
}

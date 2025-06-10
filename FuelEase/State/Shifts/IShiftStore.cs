using FuelEase.Domain.Models;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace FuelEase.State.Shifts
{
    /// <summary>
    /// Хранилище смен
    /// </summary>
    public interface IShiftStore : IHostedService
    {
        event Action<Shift> OnOpened;
        event Action<Shift> OnClosed;
        event Action<Shift> OnLogin;
        event Action<Nozzle> OnNozzleSelectionChanged;

        /// <summary>
        /// Текущая смена
        /// </summary>
        Shift CurrentShift { get; }

        /// <summary>
        /// Текущее состояние смены
        /// </summary>
        ShiftState CurrentShiftState { get; }

        /// <summary>
        /// Открыть смену
        /// </summary>
        /// <returns></returns>
        Task<bool> OpenShiftAsync();

        /// <summary>
        /// Закрыть смену
        /// </summary>
        /// <returns></returns>
        Task CloseShiftAsync();

        /// <summary>
        /// Пере открытие смену. Сначала закрывает смену потом открывает
        /// </summary>
        /// <returns></returns>
        Task ReOpeningShiftAsync();

        void NozzleSelectionChanged(Nozzle nozzle);
    }
}

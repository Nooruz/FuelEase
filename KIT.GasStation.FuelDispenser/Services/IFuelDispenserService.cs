using KIT.GasStation.HardwareConfigurations.Models;

namespace KIT.GasStation.FuelDispenser.Services
{
    public interface IFuelDispenserService : IAsyncDisposable
    {
        #region Public Properties

        ///// <summary>
        ///// Наименование типа ТРК.
        ///// </summary>
        //string DispenserName { get; }

        ///// <summary>
        ///// Версия устройства, получаемая из версии проекта.
        ///// </summary>
        //string Version { get; }

        /// <summary>
        /// Идентификатор контроллера (для групп SignalR)
        /// </summary>
        Controller Controller { get; set; }

        /// <summary>
        /// Статус ТРК.
        /// </summary>
        //ColumnStatus Status { get; }

        #endregion

        #region Public Voids

        Task RunAsync(CancellationToken token, Controller controller);

        Task StartFuelingAsync(string groupName, decimal value, bool bySum);

        #endregion
    }

    /// <summary>
    /// Тип контроллера Ланфенг.
    /// </summary>
    public enum LanfengControllerType
    {
        None,

        /// <summary>
        /// Однорукавный
        /// </summary>
        Single = 0x01,

        /// <summary>
        /// Многорукавный
        /// </summary>
        Multi = 0x04
    }
}

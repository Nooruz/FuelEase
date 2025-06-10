using FuelEase.Domain.Models;
using FuelEase.HardwareConfigurations.Models;
using System.ComponentModel;

namespace FuelEase.FuelDispenser.Services
{
    /// <summary>
    /// Интерфейс для управления топливораздаточными устройствами.
    /// </summary>
    public delegate TFuelDispenser CreateFuelDispenser<TFuelDispenser>(IEnumerable<Nozzle>? nozzles) where TFuelDispenser : IFuelDispenserService;
    public interface IFuelDispenserService : INotifyPropertyChanged, IDisposable
    {
        #region Actions

        /// <summary>
        /// Событие изменения статуса колонки.
        /// </summary>
        //event Action<Guid, ColumnStatus> OnStatusChanged;

        /// <summary>
        /// Событие получения счетчика.
        /// </summary>
        //event Action<Guid, decimal> OnCounterReceived;

        /// <summary>
        /// Событие при поднятии пистолета.
        /// </summary>
        //event Action<Guid> OnColumnLifted;

        /// <summary>
        /// Событие при опускании пистолета.
        /// </summary>
        //event Action OnColumnLowered;

        /// <summary>
        /// Событие при старте заправки.
        /// </summary>
        //event Action<Guid, decimal, decimal> OnStartedFilling;

        /// <summary>
        /// Событие при ожидании снятия пистолета.
        /// </summary>
        //event Action<int> OnWaitingRemoved;

        /// <summary>
        /// Событие при завершении заправки.
        /// </summary>
        //event Action<int> OnCompletedFilling;

        /// <summary>
        /// Событие при потере соединения.
        /// </summary>
        //event Action OnConnectionLost;

        #endregion

        #region Public Properties

        /// <summary>
        /// Наименование типа ТРК.
        /// </summary>
        string DispenserName { get; }

        /// <summary>
        /// Версия устройства, получаемая из версии проекта.
        /// </summary>
        string Version { get; }

        /// <summary>
        /// Статус ТРК.
        /// </summary>
        //ColumnStatus Status { get; }

        #endregion

        #region Public Voids

        /// <summary>
        /// Запустить постоянный поллинг статусов.
        /// </summary>
        /// <param name="intervalMs">Интервал между циклами в миллисекундах.</param>
        Task StartStatusPolling(int intervalMs = 200);

        Task Connect(string comPort, int baudRate);

        Task<NozzleStatus> CheckStatusAsync(Column column);

        Task SetPriceAsync(Nozzle nozzle, decimal? price = null);

        Task StartRefuelingSumAsync(Nozzle nozzle, decimal? sum = null);

        #endregion

        //#region Voids

        ///// <summary>
        ///// Подключение к устройству.
        ///// </summary>
        //Task Connect(string comPort, int baudRate);

        ///// <summary>
        ///// Инициализация устройства.
        ///// </summary>
        ///// <param name="columnId">Идентификатор колонки</param>
        ///// <returns></returns>
        //Task Connect(ObservableCollection<Nozzle> nozzles);

        ///// <summary>
        ///// Инициализация устройства.
        ///// </summary>
        //Task InitializeAsync(int side);

        ///// <summary>
        ///// Получение текущего статуса колонки.
        ///// </summary>
        ///// <param name="tube">Номер колонки.</param>
        ///// <returns>Текущий статус колонки.</returns>
        //Task<ColumnStatus> GetStatusAsync(int tube);


        ///// <summary>
        ///// Проверка текущего статуса колонки.
        ///// </summary>
        ///// <param name="column">Колонки.</param>
        ///// <returns>Текущий статус колонки.</returns>
        //Task<ColumnStatus> CheckStatusAsync(Column column);

        ///// <summary>
        ///// Установка цены топлива.
        ///// </summary>
        ///// <param name="price">Цена за литр.</param>
        ///// <param name="tube">Номер колонки.</param>
        ///// <returns>Задача.</returns>
        //Task SetPriceAsync(Nozzle nozzle, decimal? price = null);

        ///// <summary>
        ///// Начало заправки на определенный объем на литр.
        ///// </summary>
        ///// <param name="quantity">Объем топлива в литрах.</param>
        ///// <param name="tube">Номер колонки.</param>
        ///// <returns>Задача.</returns>
        //Task StartRefuelingQuantityAsync(Nozzle nozzle, decimal? quantity = null);

        ///// <summary>
        ///// Начало заправки на определенный объем на сумму.
        ///// </summary>
        ///// <param name="tube">Номер колонки.</param>
        ///// <param name="sum">Сумма</param>
        ///// <returns></returns>
        //Task StartRefuelingSumAsync(Nozzle nozzle, decimal? sum = null);

        ///// <summary>
        ///// Прекращение заправки.
        ///// </summary>
        ///// <returns>Задача.</returns>
        //Task StopRefuelingAsync(Nozzle nozzle);

        ///// <summary>
        ///// Продолжение заправки.
        ///// </summary>
        ///// <param name="tube">Номер колонки.</param>
        ///// <returns>Задача</returns>
        //Task ContinueFillingAsync(int tube);

        ///// <summary>
        ///// Завершение заправки.
        ///// </summary>
        ///// <param name="tube">Номер колонки.</param>
        ///// <returns>Задача</returns>
        //Task CompleteFillingAsync(int tube);

        ///// <summary>
        ///// Запрос текущих счетчиков.
        ///// </summary>
        ///// <param name="tube">Номер колонки.</param>
        //Task GetCountersAsync(Nozzle nozzle);

        ///// <summary>
        ///// Запускает фоновый цикл периодической проверки статуса колонок.
        ///// </summary>
        ///// <param name="interval">Интервал между запросами (например, 1 секунда).</param>
        ///// <returns>Задача (Task), которая будет выполняться в фоне.</returns>
        //void StartStatusLoopAsync(TimeSpan interval);

        ///// <summary>
        ///// Останавливает фоновый цикл периодической проверки статуса.
        ///// </summary>
        //void StopStatusLoop();

        //#endregion
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
